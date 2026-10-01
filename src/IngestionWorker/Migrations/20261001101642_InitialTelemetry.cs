using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IngestionWorker.Migrations
{
    /// <inheritdoc />
    public partial class InitialTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telemetry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Site = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Line = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MachineId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SpindleSpeedRpm = table.Column<double>(type: "double precision", nullable: false),
                    SpindleLoadPct = table.Column<double>(type: "double precision", nullable: false),
                    SpindleTempC = table.Column<double>(type: "double precision", nullable: false),
                    VibrationMmS = table.Column<double>(type: "double precision", nullable: false),
                    PowerKw = table.Column<double>(type: "double precision", nullable: false),
                    PartCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_MachineId_Timestamp",
                table: "telemetry",
                columns: new[] { "MachineId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telemetry");
        }
    }
}
