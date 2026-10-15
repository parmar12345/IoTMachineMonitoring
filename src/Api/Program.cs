
using Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString =
    builder.Configuration.GetConnectionString("TelemetryDb")
    ?? throw new InvalidOperationException(
        "Connection string 'TelemetryDb' was not found.");

builder.Services.AddDbContext<TelemetryReadDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Health endpoint
app.MapGet("/api/health", async (
    TelemetryReadDbContext db,
    CancellationToken cancellationToken) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync(
            cancellationToken);

        if (!canConnect)
        {
            return Results.Problem(
                title: "Database unavailable",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new
        {
            status = "Healthy",
            database = "Connected"
        });
    }
    catch (Exception)
    {
        return Results.Problem(
            title: "Database connection failed",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("GetApiHealth");

// Latest telemetry with optional filters
app.MapGet("/api/telemetry/latest", async (
    string? site,
    string? line,
    string? machineId,
    TelemetryReadDbContext db,
    CancellationToken cancellationToken) =>
{
    var sql = """
        SELECT DISTINCT ON ("Site", "Line", "MachineId") *
        FROM public.telemetry
        WHERE 1 = 1
        """;

    var parameters = new List<object>();


    if (!string.IsNullOrWhiteSpace(site))
    {
        sql += """ AND "Site" = {0}""";
        parameters.Add(site);
    }

    if (!string.IsNullOrWhiteSpace(line))
    {
        sql += " AND \"Line\" = {" + parameters.Count + "}";
        parameters.Add(line);
    }

    if (!string.IsNullOrWhiteSpace(machineId))
    {
        sql += " AND \"MachineId\" = {" + parameters.Count + "}";
        parameters.Add(machineId);
    }



    sql += "\nORDER BY\n" +
           "    \"Site\",\n" +
           "    \"Line\",\n" +
           "    \"MachineId\",\n" +
           "    \"Timestamp\" DESC,\n" +
           "    \"Sequence\" DESC";


    var query = db.Telemetry
        .FromSqlRaw(sql, parameters.ToArray())
        .AsNoTracking();

    var telemetry = await query
        .OrderBy(x => x.Site)
        .ThenBy(x => x.Line)
        .ThenBy(x => x.MachineId)
        .Select(x => new
        {
            x.Site,
            x.Line,
            x.MachineId,
            x.Timestamp,
            x.Sequence,
            x.State,
            x.SpindleSpeedRpm,
            x.SpindleLoadPct,
            x.SpindleTempC,
            x.VibrationMmS,
            x.PowerKw,
            x.PartCount
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new
    {
        count = telemetry.Count,
        items = telemetry
    });
})
.WithName("GetLatestTelemetry");

// Machine status summary
app.MapGet("/api/machines/summary", async (
    TelemetryReadDbContext db,
    CancellationToken cancellationToken) =>
{
    var latestPerMachine = db.Telemetry
        .FromSqlRaw("""
            SELECT DISTINCT ON ("Site", "Line", "MachineId") *
            FROM public.telemetry
            ORDER BY
                "Site",
                "Line",
                "MachineId",
                "Timestamp" DESC,
                "Sequence" DESC
            """)
        .AsNoTracking();

    var summary = await latestPerMachine
        .GroupBy(x => x.State)
        .Select(group => new
        {
            State = group.Key,
            Count = group.Count()
        })
        .ToListAsync(cancellationToken);

    var counts = summary.ToDictionary(
        x => x.State,
        x => x.Count,
        StringComparer.OrdinalIgnoreCase);

    counts.TryGetValue("Running", out var running);
    counts.TryGetValue("Idle", out var idle);
    counts.TryGetValue("Down", out var down);
    counts.TryGetValue("Offline", out var offline);

    return Results.Ok(new
    {
        TotalMachines = summary.Sum(x => x.Count),
        Running = running,
        Idle = idle,
        Down = down,
        Offline = offline
    });
})
.WithName("GetMachineStatusSummary");

app.Run();
