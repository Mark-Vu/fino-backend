using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fino_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddConversionJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.CreateTable(
                name: "bank_statement_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pdf_file_key = table.Column<string>(type: "text", nullable: false),
                    csv_file_key = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_statement_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_bank_statement_files_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversion_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    bank_statement_file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversion_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversion_jobs_bank_statement_files_bank_statement_file_id",
                        column: x => x.bank_statement_file_id,
                        principalTable: "bank_statement_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_files_user_id",
                table: "bank_statement_files",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversion_jobs_bank_statement_file_id",
                table: "conversion_jobs",
                column: "bank_statement_file_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversion_jobs");

            migrationBuilder.DropTable(
                name: "bank_statement_files");

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    csv_file = table.Column<string>(type: "text", nullable: false),
                    pdf_file = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_files_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_files_user_id",
                table: "files",
                column: "user_id");
        }
    }
}
