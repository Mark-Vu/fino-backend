using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fino_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantFeatureTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_conversion_jobs_uploaded_files_uploaded_file_id",
                table: "conversion_jobs");

            migrationBuilder.AddColumn<string>(
                name: "global_role",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tenant_approval_status",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tenant_role",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "uploaded_file_id",
                table: "conversion_jobs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_file_id",
                table: "conversion_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "text", nullable: false),
                    subdomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    file_extension = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_files_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id",
                table: "users",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversion_jobs_tenant_file_id",
                table: "conversion_jobs",
                column: "tenant_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_files_tenant_id",
                table: "tenant_files",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "fk_conversion_jobs_tenant_files_tenant_file_id",
                table: "conversion_jobs",
                column: "tenant_file_id",
                principalTable: "tenant_files",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_conversion_jobs_uploaded_files_uploaded_file_id",
                table: "conversion_jobs",
                column: "uploaded_file_id",
                principalTable: "uploaded_files",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_users_tenants_tenant_id",
                table: "users",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_conversion_jobs_tenant_files_tenant_file_id",
                table: "conversion_jobs");

            migrationBuilder.DropForeignKey(
                name: "fk_conversion_jobs_uploaded_files_uploaded_file_id",
                table: "conversion_jobs");

            migrationBuilder.DropForeignKey(
                name: "fk_users_tenants_tenant_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "tenant_files");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropIndex(
                name: "ix_users_tenant_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_conversion_jobs_tenant_file_id",
                table: "conversion_jobs");

            migrationBuilder.DropColumn(
                name: "global_role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "tenant_approval_status",
                table: "users");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "tenant_role",
                table: "users");

            migrationBuilder.DropColumn(
                name: "tenant_file_id",
                table: "conversion_jobs");

            migrationBuilder.AlterColumn<Guid>(
                name: "uploaded_file_id",
                table: "conversion_jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_conversion_jobs_uploaded_files_uploaded_file_id",
                table: "conversion_jobs",
                column: "uploaded_file_id",
                principalTable: "uploaded_files",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
