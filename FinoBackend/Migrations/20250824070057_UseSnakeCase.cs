using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fino_backend.Migrations
{
    /// <inheritdoc />
    public partial class UseSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "users",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "files",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "files",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "files",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_files_user_id",
                table: "files",
                newName: "ix_files_user_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                table: "users",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_files",
                table: "files",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_files_users_user_id",
                table: "files",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_files_users_user_id",
                table: "files");

            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "pk_files",
                table: "files");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "files",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "files",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "files",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_files_user_id",
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
    }
}
