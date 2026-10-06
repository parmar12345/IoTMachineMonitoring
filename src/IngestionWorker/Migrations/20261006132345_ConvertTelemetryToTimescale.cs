using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IngestionWorker.Migrations
{
    /// <inheritdoc />
    public partial class ConvertTelemetryToTimescale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_telemetry",
                table: "telemetry");

            migrationBuilder.AddPrimaryKey(
                name: "PK_telemetry",
                table: "telemetry",
                columns: new[] { "Id", "Timestamp" });

            migrationBuilder.Sql("""
        SELECT create_hypertable(
            'telemetry',
            by_range('Timestamp'),
            migrate_data => TRUE
        );
        """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_telemetry",
                table: "telemetry");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "telemetry",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_telemetry",
                table: "telemetry",
                column: "Id");
        }
    }
}
