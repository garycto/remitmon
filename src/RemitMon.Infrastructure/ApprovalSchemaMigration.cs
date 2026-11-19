namespace RemitMon.Infrastructure;

public static class ApprovalSchemaMigration
{
    public const string RenameLegacyTableSql = """
        DO $$
        BEGIN
            IF to_regclass('public.exceptions') IS NOT NULL THEN
                IF to_regclass('public.approvals') IS NOT NULL THEN
                    RAISE EXCEPTION 'Both legacy exceptions and approvals tables exist.';
                END IF;

                ALTER TABLE public.exceptions RENAME TO approvals;
                ALTER TABLE public.approvals RENAME CONSTRAINT "PK_exceptions" TO "PK_approvals";
                ALTER TABLE public.approvals RENAME CONSTRAINT "FK_exceptions_decisions_decision_id" TO "FK_approvals_decisions_decision_id";
                ALTER INDEX public."IX_exceptions_decision_id" RENAME TO "IX_approvals_decision_id";
            END IF;
        END
        $$;
        """;

    public const string RestoreLegacyTableSql = """
        DO $$
        BEGIN
            IF to_regclass('public.approvals') IS NOT NULL THEN
                IF to_regclass('public.exceptions') IS NOT NULL THEN
                    RAISE EXCEPTION 'Both approvals and legacy exceptions tables exist.';
                END IF;

                ALTER TABLE public.approvals RENAME CONSTRAINT "PK_approvals" TO "PK_exceptions";
                ALTER TABLE public.approvals RENAME CONSTRAINT "FK_approvals_decisions_decision_id" TO "FK_exceptions_decisions_decision_id";
                ALTER INDEX public."IX_approvals_decision_id" RENAME TO "IX_exceptions_decision_id";
                ALTER TABLE public.approvals RENAME TO exceptions;
            END IF;
        END
        $$;
        """;
}
