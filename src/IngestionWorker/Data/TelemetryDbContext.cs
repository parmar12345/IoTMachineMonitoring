using Microsoft.EntityFrameworkCore;

namespace IngestionWorker.Data;

public sealed class TelemetryDbContext : DbContext
{
    public TelemetryDbContext(
        DbContextOptions<TelemetryDbContext> options)
        : base(options)
    {
    }

    public DbSet<TelemetryRecord> Telemetry =>
        Set<TelemetryRecord>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryRecord>(
            entity =>
            {
                entity.ToTable("telemetry");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Site)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Line)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.MachineId)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.State)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.HasIndex(x => new
                {
                    x.MachineId,
                    x.Timestamp
                });
            });
    }
}