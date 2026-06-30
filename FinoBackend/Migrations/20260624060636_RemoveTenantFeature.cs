using System;
using FinoBackend.Commons.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fino_backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTenantFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_conversion_jobs_tenant_files_tenant_file_id",
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

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:file_category", "bank_statement,delivery_receipt")
                .Annotation("Npgsql:Enum:file_extension", "jpg,pdf,png,tiff,xlsx")
                .Annotation("Npgsql:Enum:global_role", "developer,super_admin,user")
                .Annotation("Npgsql:Enum:owner_type", "anonymous,authenticated_user")
                .Annotation("Npgsql:Enum:queue_type", "bank_statement_conversion,delivery_receipt_conversion,public_bank_statement_conversion")
                .OldAnnotation("Npgsql:Enum:file_category", "bank_statement,delivery_receipt")
                .OldAnnotation("Npgsql:Enum:file_extension", "jpg,pdf,png,tiff,xlsx")
                .OldAnnotation("Npgsql:Enum:global_role", "developer,super_admin,user")
                .OldAnnotation("Npgsql:Enum:owner_type", "anonymous,authenticated_user")
                .OldAnnotation("Npgsql:Enum:queue_type", "bank_statement_conversion,delivery_receipt_conversion,public_bank_statement_conversion")
                .OldAnnotation("Npgsql:Enum:tenant_approval_status", "approved,pending,rejected")
                .OldAnnotation("Npgsql:Enum:tenant_role", "admin,member");

            migrationBuilder.Sql("DROP TYPE IF EXISTS tenant_approval_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS tenant_role;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:file_category", "bank_statement,delivery_receipt")
                .Annotation("Npgsql:Enum:file_extension", "jpg,pdf,png,tiff,xlsx")
                .Annotation("Npgsql:Enum:global_role", "developer,super_admin,user")
                .Annotation("Npgsql:Enum:owner_type", "anonymous,authenticated_user")
                .Annotation("Npgsql:Enum:queue_type", "bank_statement_conversion,delivery_receipt_conversion,public_bank_statement_conversion")
                .Annotation("Npgsql:Enum:tenant_approval_status", "approved,pending,rejected")
                .Annotation("Npgsql:Enum:tenant_role", "admin,member")
                .OldAnnotation("Npgsql:Enum:file_category", "bank_statement,delivery_receipt")
                .OldAnnotation("Npgsql:Enum:file_extension", "jpg,pdf,png,tiff,xlsx")
                .OldAnnotation("Npgsql:Enum:global_role", "developer,super_admin,user")
                .OldAnnotation("Npgsql:Enum:owner_type", "anonymous,authenticated_user")
                .OldAnnotation("Npgsql:Enum:queue_type", "bank_statement_conversion,delivery_receipt_conversion,public_bank_statement_conversion");

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'tenant_approval_status') THEN
                        CREATE TYPE tenant_approval_status AS ENUM ('pending', 'approved', 'rejected');
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'tenant_role') THEN
                        CREATE TYPE tenant_role AS ENUM ('admin', 'member');
                    END IF;
                END $$;
                """);

            migrationBuilder.AddColumn<int>(
                name: "tenant_approval_status",
                table: "users",
                type: "tenant_approval_status",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tenant_role",
                table: "users",
                type: "tenant_role",
                nullable: true);

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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    subdomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_extension = table.Column<FileExtension>(type: "file_extension", nullable: false),
                    file_key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "ix_tenants_company_name",
                table: "tenants",
                column: "company_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenants_subdomain",
                table: "tenants",
                column: "subdomain",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_conversion_jobs_tenant_files_tenant_file_id",
                table: "conversion_jobs",
                column: "tenant_file_id",
                principalTable: "tenant_files",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_users_tenants_tenant_id",
                table: "users",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id");
        }
    }
}
