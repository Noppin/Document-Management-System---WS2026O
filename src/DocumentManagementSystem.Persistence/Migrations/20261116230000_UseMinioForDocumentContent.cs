using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DocumentManagementSystem.Persistence.Migrations;

[DbContext(typeof(DocumentManagementDbContext))]
[Migration("20261116230000_UseMinioForDocumentContent")]
public sealed class UseMinioForDocumentContent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AlterColumn<byte[]>(
            name: "Content", table: "documents", type: "bytea", nullable: true,
            oldClrType: typeof(byte[]), oldType: "bytea");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "PDFs now live in MinIO. Restore them to PostgreSQL before rolling back this migration.");
}
