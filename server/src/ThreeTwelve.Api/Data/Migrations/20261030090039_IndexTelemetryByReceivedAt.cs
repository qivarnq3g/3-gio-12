using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreeTwelve.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndexTelemetryByReceivedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_telemetry_events_occurred_at_type",
                table: "telemetry_events");

            migrationBuilder.CreateIndex(
                name: "ix_telemetry_events_received_at_type",
                table: "telemetry_events",
                columns: new[] { "received_at", "type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_telemetry_events_received_at_type",
                table: "telemetry_events");

            migrationBuilder.CreateIndex(
                name: "ix_telemetry_events_occurred_at_type",
                table: "telemetry_events",
                columns: new[] { "occurred_at", "type" });
        }
    }
}
