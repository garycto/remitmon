using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RemitMon.Infrastructure;

namespace RemitMon.Infrastructure.Migrations;

[DbContext(typeof(RemitMonDbContext))]
[Migration("20261118215200_RenameExceptionsToApprovals")]
public partial class RenameExceptionsToApprovals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(ApprovalSchemaMigration.RenameLegacyTableSql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(ApprovalSchemaMigration.RestoreLegacyTableSql);
    }
}
