using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RemitMon.Infrastructure.Migrations;

[DbContext(typeof(RemitMonDbContext))]
[Migration("20261118220000_AddPaymentEventIdempotency")]
public partial class AddPaymentEventIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            LOCK TABLE payment_events, expected_obligations IN ACCESS EXCLUSIVE MODE;
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM payment_events WHERE amount <> round(amount, 2) OR abs(amount) >= 10000000000000000)
                   OR EXISTS (SELECT 1 FROM expected_obligations WHERE amount <> round(amount, 2) OR abs(amount) >= 10000000000000000) THEN
                    RAISE EXCEPTION 'Existing monetary values cannot be represented as numeric(18,2) without loss.';
                END IF;
            END
            $$;
            """);

        migrationBuilder.AlterColumn<decimal>(
            name: "amount", table: "payment_events", type: "numeric(18,2)",
            precision: 18, scale: 2, nullable: false, oldClrType: typeof(decimal), oldType: "numeric");
        migrationBuilder.AlterColumn<decimal>(
            name: "amount", table: "expected_obligations", type: "numeric(18,2)",
            precision: 18, scale: 2, nullable: false, oldClrType: typeof(decimal), oldType: "numeric");
        migrationBuilder.AddColumn<string>(
            name: "external_event_id", table: "payment_events", type: "text", nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_payment_events_external_event_id", table: "payment_events",
            column: "external_event_id", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_payment_events_external_event_id", table: "payment_events");
        migrationBuilder.DropColumn(name: "external_event_id", table: "payment_events");
        migrationBuilder.AlterColumn<decimal>(
            name: "amount", table: "payment_events", type: "numeric",
            nullable: false, oldClrType: typeof(decimal), oldType: "numeric(18,2)", oldPrecision: 18, oldScale: 2);
        migrationBuilder.AlterColumn<decimal>(
            name: "amount", table: "expected_obligations", type: "numeric",
            nullable: false, oldClrType: typeof(decimal), oldType: "numeric(18,2)", oldPrecision: 18, oldScale: 2);
    }
}
