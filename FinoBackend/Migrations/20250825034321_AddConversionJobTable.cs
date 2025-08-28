using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fino_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddConversionJobTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "files");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "files",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
