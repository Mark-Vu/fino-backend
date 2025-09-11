using System;
using FinoBackend.Commons.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fino_backend.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:file_category", "bank_statement,delivery_receipt")
                .Annotation("Npgsql:Enum:file_extension", "jpg,pdf,png,tiff,xlsx")
                .Annotation("Npgsql:Enum:global_role", "developer,super_admin,user")
                .Annotation("Npgsql:Enum:owner_type", "anonymous,authenticated_user")
                .Annotation("Npgsql:Enum:queue_type", "bank_statement_conversion,delivery_receipt_conversion,public_bank_statement_conversion")
                .Annotation("Npgsql:Enum:tenant_approval_status", "approved,pending,rejected")
                .Annotation("Npgsql:Enum:tenant_role", "admin,member");

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
                name: "uploaded_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_type = table.Column<OwnerType>(type: "owner_type", nullable: false),
                    uploaded_file_key = table.Column<string>(type: "text", nullable: false),
                    file_extension = table.Column<FileExtension>(type: "file_extension", nullable: false),
                    category = table.Column<FileCategory>(type: "file_category", nullable: false),
                    original_file_name = table.Column<string>(type: "text", nullable: false),
                    output_file_key = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_uploaded_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    file_extension = table.Column<FileExtension>(type: "file_extension", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    global_role = table.Column<GlobalRole>(type: "global_role", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_role = table.Column<TenantRole>(type: "tenant_role", nullable: true),
                    tenant_approval_status = table.Column<TenantApprovalStatus>(type: "tenant_approval_status", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "conversion_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    uploaded_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversion_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversion_jobs_tenant_files_tenant_file_id",
                        column: x => x.tenant_file_id,
                        principalTable: "tenant_files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_conversion_jobs_uploaded_files_uploaded_file_id",
                        column: x => x.uploaded_file_id,
                        principalTable: "uploaded_files",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_conversion_jobs_tenant_file_id",
                table: "conversion_jobs",
                column: "tenant_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversion_jobs_uploaded_file_id",
                table: "conversion_jobs",
                column: "uploaded_file_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_users_tenant_id",
                table: "users",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversion_jobs");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "tenant_files");

            migrationBuilder.DropTable(
                name: "uploaded_files");

            migrationBuilder.DropTable(
                name: "tenants");
        }
    }
}
