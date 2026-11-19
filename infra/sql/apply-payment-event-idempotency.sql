-- Run once in the Supabase SQL editor. All changes are transactional.
BEGIN;

LOCK TABLE public.payment_events, public.expected_obligations IN ACCESS EXCLUSIVE MODE;

DO $$
BEGIN
    IF to_regclass('public."__EFMigrationsHistory"') IS NOT NULL THEN
        IF NOT EXISTS (
            SELECT 1 FROM public."__EFMigrationsHistory"
            WHERE "MigrationId" = '20261118204428_InitialCreate'
        ) THEN
            RAISE EXCEPTION 'EF migration history exists but the RemitMon initial migration is not recorded. No changes were made.';
        END IF;

        IF EXISTS (
            SELECT 1 FROM public."__EFMigrationsHistory"
            WHERE "MigrationId" = '20261118220000_AddPaymentEventIdempotency'
        ) THEN
            RAISE EXCEPTION 'The payment event idempotency migration is already recorded. No changes were made.';
        END IF;

        IF NOT EXISTS (
            SELECT 1 FROM public."__EFMigrationsHistory"
            WHERE "MigrationId" = '20261118215200_RenameExceptionsToApprovals'
        ) THEN
            IF to_regclass('public.exceptions') IS NOT NULL THEN
                IF to_regclass('public.approvals') IS NOT NULL THEN
                    RAISE EXCEPTION 'Both legacy exceptions and approvals tables exist. No changes were made.';
                END IF;

                ALTER TABLE public.exceptions RENAME TO approvals;
                ALTER TABLE public.approvals RENAME CONSTRAINT "PK_exceptions" TO "PK_approvals";
                ALTER TABLE public.approvals RENAME CONSTRAINT "FK_exceptions_decisions_decision_id" TO "FK_approvals_decisions_decision_id";
                ALTER INDEX public."IX_exceptions_decision_id" RENAME TO "IX_approvals_decision_id";
            ELSIF to_regclass('public.approvals') IS NULL THEN
                RAISE EXCEPTION 'Neither the legacy exceptions table nor approvals exists. No changes were made.';
            END IF;

            INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('20261118215200_RenameExceptionsToApprovals', '9.0.0');
        END IF;
    END IF;

    IF EXISTS (
        SELECT 1 FROM public.payment_events
        WHERE amount <> round(amount, 2) OR abs(amount) >= 10000000000000000
    ) OR EXISTS (
        SELECT 1 FROM public.expected_obligations
        WHERE amount <> round(amount, 2) OR abs(amount) >= 10000000000000000
    ) THEN
        RAISE EXCEPTION 'Existing monetary values cannot be represented as numeric(18,2) without loss.';
    END IF;
END
$$;

ALTER TABLE public.payment_events
    ALTER COLUMN amount TYPE numeric(18,2);
ALTER TABLE public.expected_obligations
    ALTER COLUMN amount TYPE numeric(18,2);
ALTER TABLE public.payment_events
    ADD COLUMN external_event_id text;
CREATE UNIQUE INDEX "IX_payment_events_external_event_id"
    ON public.payment_events (external_event_id);

DO $$
BEGIN
    IF to_regclass('public."__EFMigrationsHistory"') IS NOT NULL THEN
        INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20261118220000_AddPaymentEventIdempotency', '9.0.0');
    END IF;
END
$$;

COMMIT;
