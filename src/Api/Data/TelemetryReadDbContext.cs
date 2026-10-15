
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public sealed class TelemetryReadDbContext : DbContext
{
    public TelemetryReadDbContext(
        DbContextOptions<TelemetryReadDbContext> options)
        : base(options)
    {
    }

    public DbSet<TelemetryReadRecord> Telemetry =>
        Set<TelemetryReadRecord>();

    public DbSet<LatestTelemetryReadRecord> LatestTelemetry
    => Set<LatestTelemetryReadRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryReadRecord>(entity =>
        {
            entity.ToTable("telemetry", "public");

            // The existing TimescaleDB table uses a composite primary key.
            entity.HasKey(x => new { x.Id, x.Timestamp });

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Site).HasMaxLength(50);
            entity.Property(x => x.Line).HasMaxLength(50);
            entity.Property(x => x.MachineId).HasMaxLength(50);
            entity.Property(x => x.State).HasMaxLength(20);

            entity.HasIndex(x => new { x.MachineId, x.Timestamp });
        });

        modelBuilder.Entity<LatestTelemetryReadRecord>(entity =>
        {
            entity.ToTable("machine_latest_telemetry", "public");

            entity.HasKey(x => new
            {
                x.Site,
                x.Line,
                x.MachineId
            });

            entity.Property(x => x.Site).HasMaxLength(50);
            entity.Property(x => x.Line).HasMaxLength(50);
            entity.Property(x => x.MachineId).HasMaxLength(50);
            entity.Property(x => x.Timestamp);
            entity.Property(x => x.State).HasMaxLength(20);
        });
    }
}
