using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fino_backend.Migrations
{
    /// <inheritdoc />
    public partial class RenameCols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Rename table
            migrationBuilder.RenameTable(
                name: "bank_statement_files",
                newName: "uploaded_files");

            // 2. Rename the column in conversion_jobs
            migrationBuilder.RenameColumn(
                name: "bank_statement_file_id",
                table: "conversion_jobs",
                newName: "uploaded_file_id");

            // 3. Add new foreign key (don’t drop old one, since it’s already gone)
            migrationBuilder.AddForeignKey(
                name: "fk_conversion_jobs_uploaded_files_uploaded_file_id",
                table: "conversion_jobs",
                column: "uploaded_file_id",
                principalTable: "uploaded_files",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_conversion_jobs_uploaded_files_uploaded_file_id",
                table: "conversion_jobs");

            migrationBuilder.RenameColumn(
                name: "uploaded_file_id",
                table: "conversion_jobs",
                newName: "bank_statement_file_id");

            migrationBuilder.RenameTable(
                name: "uploaded_files",
                newName: "bank_statement_files");

            migrationBuilder.AddForeignKey(
                name: "fk_conversion_jobs_bank_statement_files_bank_statement_file_id",
                table: "conversion_jobs",
                column: "bank_statement_file_id",
                principalTable: "bank_statement_files",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

    }
}
