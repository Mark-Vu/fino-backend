using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fino_backend.Migrations
{
    /// <inheritdoc />
    public partial class ColumnNameChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Users_userId",
                table: "Files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Files",
                table: "Files");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Files",
                newName: "files");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "users",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "files",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "userId",
                table: "files",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "PdfFile",
                table: "files",
                newName: "pdf_file");

            migrationBuilder.RenameColumn(
                name: "CsvFile",
                table: "files",
                newName: "csv_file");

            migrationBuilder.RenameIndex(
                name: "IX_Files_userId",
                table: "files",
                newName: "IX_files_user_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_files",
                table: "files",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_files_users_user_id",
                table: "files",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_files_users_user_id",
                table: "files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_files",
                table: "files");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "files",
                newName: "Files");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Users",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Files",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "Files",
                newName: "userId");

            migrationBuilder.RenameColumn(
                name: "pdf_file",
                table: "Files",
                newName: "PdfFile");

            migrationBuilder.RenameColumn(
                name: "csv_file",
                table: "Files",
                newName: "CsvFile");

            migrationBuilder.RenameIndex(
                name: "IX_files_user_id",
                table: "Files",
                newName: "IX_Files_userId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Files",
                table: "Files",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Users_userId",
                table: "Files",
                column: "userId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
