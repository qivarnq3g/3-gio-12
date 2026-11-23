using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThreeTwelve.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPuzzles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "puzzles",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    unlock_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    max_failed_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    attempt_window_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_puzzles", x => x.id);
                    table.CheckConstraint("ck_puzzles_attempt_window_minutes", "attempt_window_minutes between 1 and 10080");
                    table.CheckConstraint("ck_puzzles_max_failed_attempts", "max_failed_attempts between 1 and 100");
                });

            migrationBuilder.CreateTable(
                name: "puzzle_solutions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    puzzle_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sha256 = table.Column<string>(type: "character(64)", nullable: false),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_puzzle_solutions", x => x.id);
                    table.CheckConstraint("ck_puzzle_solutions_sha256", "sha256 ~ '^[0-9a-f]{64}$'");
                    table.ForeignKey(
                        name: "fk_puzzle_solutions_puzzles_puzzle_id",
                        column: x => x.puzzle_id,
                        principalTable: "puzzles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "puzzle_submissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    puzzle_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    installation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sha256 = table.Column<string>(type: "character(64)", nullable: false),
                    result = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    game_version = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_puzzle_submissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_puzzle_submissions_puzzles_puzzle_id",
                        column: x => x.puzzle_id,
                        principalTable: "puzzles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "puzzles",
                columns: new[] { "id", "attempt_window_minutes", "max_failed_attempts", "title", "unlock_id", "updated_at", "updated_by_id" },
                values: new object[] { "red-thread-sandal", 60, 5, "Đôi dép chỉ đỏ", "ending_red_thread_sandal", new DateTimeOffset(new DateTime(2026, 10, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });

            migrationBuilder.CreateIndex(
                name: "ix_puzzle_solutions_puzzle_id_sha256",
                table: "puzzle_solutions",
                columns: new[] { "puzzle_id", "sha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_puzzle_submissions_puzzle_id_installation_id_submitted_at",
                table: "puzzle_submissions",
                columns: new[] { "puzzle_id", "installation_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_puzzle_submissions_puzzle_id_submitted_at",
                table: "puzzle_submissions",
                columns: new[] { "puzzle_id", "submitted_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "puzzle_solutions");

            migrationBuilder.DropTable(
                name: "puzzle_submissions");

            migrationBuilder.DropTable(
                name: "puzzles");
        }
    }
}
