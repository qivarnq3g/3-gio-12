using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThreeTwelve.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telemetry_events",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    game_version = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    night = table.Column<short>(type: "smallint", nullable: true),
                    area_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    checkpoint_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    choice_id = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    ending_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    end_reason = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    is_replay = table.Column<bool>(type: "boolean", nullable: true),
                    position_x = table.Column<double>(type: "double precision", nullable: true),
                    position_y = table.Column<double>(type: "double precision", nullable: true),
                    position_z = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telemetry_events", x => x.event_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_telemetry_events_installation_id",
                table: "telemetry_events",
                column: "installation_id");

            migrationBuilder.CreateIndex(
                name: "ix_telemetry_events_occurred_at_type",
                table: "telemetry_events",
                columns: new[] { "occurred_at", "type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telemetry_events");
        }
    }
}
