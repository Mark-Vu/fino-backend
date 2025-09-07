using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fino_backend.Migrations
{
    /// <inheritdoc />
    public partial class ChangeColName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "pdf_file_key",
                table: "bank_statement_files",
                newName: "uploaded_file_key");

            migrationBuilder.AddColumn<int>(
                name: "file_extension",
                table: "bank_statement_files",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "file_extension",
                table: "bank_statement_files");

            migrationBuilder.RenameColumn(
                name: "uploaded_file_key",
                table: "bank_statement_files",
                newName: "pdf_file_key");
        }
    }
}
