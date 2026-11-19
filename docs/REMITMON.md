# RemitMon (RM) — Build Specification for GitHub Copilot

> Copilot: build this as a working prototype in this repository. Follow the stack, structure, rules, and build order below. Produce complete files (no diffs, no placeholders, no "TODO: implement"). C# only for backend and tooling (no Python). When a requirement is ambiguous, choose the simplest option consistent with this document and record the choice in `docs/DECISIONS.md`.

---

## 1. Product summary

RemitMon is a bank-distributed account protection platform. It places configurable, deterministic financial guardrails around business bank accounts so that AI agents, automated purchasing systems, and software integrations can move money only within defined boundaries.

RM watches ACH (especially **ACH pulls / debits**), RTP, FedNow, and wire activity, plus **Requests for Payment (RfP)**. It compares each item against (a) customer-defined policy and (b) expected obligations learned from accounting software webhooks (bill / invoice created or received).

Outcome per item: **ALLOW**, **REJECT**, or **ESCALATE** (human approval). Where a payment already moved, RM can attempt **REVERSE / RECALL** where the rail permits.

Key premises (do not deviate):
1. RM is a **rule-based, deterministic enforcement engine** (similar to a network firewall). No ML scoring in the decision path. Same input + same policy version = same decision, always.
2. RM acts at **point of payment**, not only at initiation. Inbound debits and requests are evaluated before posting/settlement wherever the rail allows.
3. **RfP flows** have a native pause between request and payment. RM uses that pause for evaluation, human approval, and automated acceptance.
4. **ACH pulls** are a primary use case: the payee initiates, the payer has no approval step. Recourse is **reject (return before posting)** or **reverse/return** where permitted.
5. **Accounting webhooks** (QuickBooks first) announce invoice/bill creation or receipt, producing **expected obligations** before money moves. A matching debit/request is auto-cleared; one with no matching obligation requires approval.
6. The bank distributes RM to its business customers. The prototype has three views: **Business**, **Bank**, and **Admin/Simulator**.

This is a **prototype**. All bank, rail, and accounting integrations are behind interfaces with **simulator implementations** using fictional data. Nothing is sent to real banks, Jack Henry, Modern Treasury, or QuickBooks unless a real adapter is explicitly configured. Mark all simulated actions clearly in the UI ("Simulated").

---

## 2. Stack and hosting

| Layer | Choice |
|---|---|
| Frontend | React 18 + TypeScript + Vite, React Router, TanStack Query, Tailwind CSS, Recharts for charts |
| Backend | C# / .NET 8 ASP.NET Core Web API (controllers or minimal APIs; pick one and be consistent) |
| Engine | Separate class library `RemitMon.Engine` with zero dependencies on ASP.NET or EF Core |
| Data | PostgreSQL via EF Core (Npgsql). Azure Database for PostgreSQL Flexible Server in Azure; Docker Postgres locally |
| Messaging | Azure Service Bus (queue per ingest type). Locally, an in-memory channel implementation behind the same interface |
| Auth | Microsoft Entra ID (JWT bearer) in Azure; a local dev auth handler with seeded users (Business Admin, Business Approver, Bank Admin, Platform Admin) |
| Secrets | Azure Key Vault + managed identity |
| Observability | OpenTelemetry → Application Insights; structured logging (Serilog) |
| Hosting | Frontend: Azure Static Web Apps. API: Azure Container Apps (or App Service). Website and API deploy separately |
| IaC | Bicep in `/infra` |
| CI/CD | GitHub Actions: build, test, OpenAPI check, deploy frontend and API separately |
| Tests | xUnit + FluentAssertions for backend; Vitest + React Testing Library for frontend; Playwright for 3 smoke flows |

Development environment: GitHub Codespaces (primary) and local. Provide `.devcontainer/devcontainer.json` with .NET 8 SDK, Node 20, and a Postgres service.

---

## 3. Repository layout

```
/remitmon
  /.devcontainer
  /.github/workflows        ci.yml, deploy-api.yml, deploy-web.yml
  /docs                     DECISIONS.md, ARCHITECTURE.md, openapi.json (generated)
  /infra                    main.bicep, modules/*.bicep, parameters.*.json
  /src
    /RemitMon.Engine        pure policy engine (no external deps)
    /RemitMon.Domain        entities, enums, value objects
    /RemitMon.Api           ASP.NET Core host, controllers, auth, DI
    /RemitMon.Infrastructure  EF Core, Service Bus, adapters (simulator + real stubs)
    /RemitMon.Simulator     scenario runner, traffic generator, fictional data seeders
    /RemitMon.Tests         xUnit (engine, api, integration)
  /web                      React + Vite app
  RemitMon.sln
  /docs/PROJECT-OVERVIEW.md
  /docs/README-DEMO.md
```

---

## 4. Domain model

Use these entities (EF Core, snake_case table names, UUID primary keys, `created_at`/`updated_at` UTC on all). Money is `decimal(18,2)`; currency is USD only in the prototype.

**Tenancy and structure**
- `Bank` (id, name, routing_number, settings JSON)
- `BusinessCustomer` (id, bank_id, legal_name, status)
- `Account` (id, business_id, bank_id, display_name, masked_account_number, status, mode: `Monitor` | `Enforce`)
- `User` (id, entra_object_id, email, role, business_id?, bank_id?)

**Counterparties**
- `Counterparty` (id, business_id, name, status: `Pending` | `Approved` | `Blocked`, ach_company_ids[], routing_account_fingerprints[], tax_id_hash?, created_by, approved_by, approved_at, source: `Manual` | `Accounting` | `Observed`)
- `CounterpartyChangeLog` (id, counterparty_id, field, old_value, new_value, changed_by, changed_at) — changes to payee bank details are first-class risk signals

**Policy**
- `Policy` (id, account_id, name, status: `Draft` | `Active` | `Archived`, version int, effective_from, created_by, approved_by)
- `PolicyRule` (id, policy_id, scope_type: `Account` | `Counterparty` | `PaymentType` | `Transaction`, scope_ref, rule_type, parameters JSON, action: `Allow` | `Reject` | `Escalate`, priority int, enabled)
- Policies are **immutable once Active**. Edits create a new version. Activating a new version requires a second approver (four-eyes) and is itself audited.

**Activity and obligations**
- `PaymentEvent` (id, account_id, rail, direction: `Inbound` | `Outbound`, initiation: `Push` | `Pull` | `RfP`, amount, currency, counterparty_ref (raw), counterparty_id?, ach_company_id?, ach_sec_code?, memo, reference, invoice_ref?, event_time, settlement_date?, raw_payload JSON, correlation_id)
- `ExpectedObligation` (id, business_id, source: `QuickBooks` | `Simulator` | `Manual`, source_ref, counterparty_id?, counterparty_name, amount, due_date, status: `Open` | `Matched` | `Paid` | `Cancelled`, created_event_at, created_by_ext?, matched_payment_event_id?)
- `Decision` (id, payment_event_id, policy_id, policy_version, outcome: `Allow` | `Reject` | `Escalate`, reasons JSON[] (rule ids + human text), evaluated_at, latency_ms, mode_applied: `Monitor` | `Enforce`, action_taken, action_status)
- `Approval` (id, decision_id, status: `Open` | `Approved` | `Rejected` | `Expired`, assigned_to, expires_at, resolution_note, resolved_by, resolved_at)
- `ActionRecord` (id, decision_id, action: `Reject` | `Return` | `Reverse` | `Recall` | `HoldRfP` | `AcceptRfP` | `DeclineRfP`, rail, status: `Requested` | `Simulated` | `Succeeded` | `Failed` | `NotAvailable`, window_deadline, detail JSON)
- `AuditEntry` (id, actor, actor_type: `User` | `System`, entity_type, entity_id, action, before JSON, after JSON, occurred_at, prev_hash, hash) — append-only, **hash-chained** (SHA-256 of prev_hash + canonical JSON). Add a verification endpoint.

---

## 5. Policy engine (`RemitMon.Engine`)

### 5.1 Contract

```csharp
public interface IPolicyEngine
{
    EvaluationResult Evaluate(PaymentContext context, PolicySnapshot policy);
}
```

- `PaymentContext` is an immutable record containing the event, the resolved counterparty (if any), running aggregates (cumulative exposure, velocity counters supplied by the caller from a store), matched obligation (if any), and evaluation time (injected clock, never `DateTime.Now`).
- `PolicySnapshot` is an immutable, versioned set of rules.
- `EvaluationResult` = outcome + ordered list of `RuleHit` (rule id, rule type, matched, result, explanation text) + `PolicyVersion`.
- The engine is **pure**: no I/O, no randomness, no clock access. Fully unit-testable.

### 5.2 Rule types (all required)

| Rule type | Parameters | Notes |
|---|---|---|
| `CounterpartyAllowlist` | mode: allowlist/blocklist, ids | Unknown counterparty → configured action (default Escalate) |
| `PaymentTypeAllowed` | rails[], initiation[] (Push/Pull/RfP) | e.g. block ACH pulls except from approved counterparties |
| `MaxAmountPerTransaction` | amount, scope (account/counterparty/payment type) | |
| `MaxAmountPerCounterparty` | amount, period | |
| `CumulativeExposure` | amount, period (day/week/month/rolling N days), scope, include_outstanding | Includes authorized-but-unsettled items |
| `Velocity` | max_count, max_total, window (minutes/hours/days), scope | Sliding window |
| `TimeWindow` | allowed days, start/end time, time zone, holidays calendar | Banking-day aware |
| `Purpose` | require_obligation_match, amount_tolerance (abs/%), date_tolerance_days, allowed_categories, require_invoice_ref | Matches against `ExpectedObligation` |
| `NewPayeeRisk` | min_age_hours for counterparties, flag on payee bank detail change within N days, flag new-vendor + new-bill created within N hours | Signals from accounting webhooks |
| `Authorization` | auto_approve_below, require_human_above, approver_roles, expiry | Controls Allow vs Escalate |

### 5.3 Resolution semantics (document in `ARCHITECTURE.md` and implement exactly)

1. Collect all enabled rules whose scope applies to the event. Scope specificity order: **Transaction > Counterparty > PaymentType > Account**.
2. Evaluate every applicable rule (no short-circuit; the full hit list is stored for explainability).
3. Combine: any `Reject` hit → **Reject**. Else any `Escalate` hit → **Escalate**. Else **Allow**. A more specific scope may **loosen** a less specific `Escalate` (but never a `Reject` from a rule marked `Locked` at account level). Implement `Locked` as a rule flag.
4. Default when no rule applies: configurable per account (`DefaultOutcome`, default `Escalate`).
5. `Monitor` mode: compute and store the decision, apply no action, mark `mode_applied = Monitor`. `Enforce` mode: apply action.
6. Engine errors or missing data → **fail safe**: for pulls and RfP, outcome is `Reject`/`DeclineRfP` or `Escalate` per account setting `FailSafeOutcome`; never silently Allow.

### 5.4 Matching service (`RemitMon.Engine.Matching`)

Match inbound item to `ExpectedObligation` by: counterparty (id or fuzzy name), amount within tolerance, due date within tolerance, invoice ref when present. Return `Exact`, `Partial`, or `None` with a score breakdown. Deterministic scoring only (documented weights). Recurring standing authorizations (utilities, payroll providers, tax) are modeled as `StandingAuthorization` (counterparty, max amount, frequency) so debits with no bill can still be allowed.

---

## 6. Rails, actions, and timing

Model rails: `ACH`, `RTP`, `FedNow`, `Wire`. Model initiations: `Push`, `Pull`, `RfP`.

Recourse by rail (encode as configuration, **not hard-coded**, because rules and windows are assumptions to be verified with a bank/Nacha):

| Item | Primary action | Window (configurable defaults) |
|---|---|---|
| ACH pull (debit) before posting | `Reject` / return as unauthorized or per return code mapping | Pre-posting; then short post-settlement unauthorized-return window (default 2 banking days for corporate, configurable) |
| ACH credit originated by the business | `Reverse` (limited error cases) | Default 5 banking days, configurable |
| RfP (RTP / FedNow) | `HoldRfP` → `AcceptRfP` or `DeclineRfP` | Until RfP expiry timestamp |
| RTP / FedNow push | Allow/Block at point of payment; `Recall` request after the fact is bank-cooperation dependent | Mark `NotAvailable` or `Requested` |
| Wire | Hold / `Recall` request | Bank-cooperation dependent |

`ActionRecord.window_deadline` must be computed using a **banking-day calendar** service (`IBankingCalendar`, Federal Reserve holiday set seeded). The UI shows a countdown to the deadline for any pending approval that has a recourse window.

Escalation behavior:
- RfP: pending until expiry; auto-decline on expiry (fail safe).
- ACH pull pre-posting: pending until the cutoff derived from the posting schedule; auto-reject at cutoff.
- Push rails: no indefinite pend; the escalation outcome becomes reject-with-resubmit (state this in UI copy).

---

## 7. Integrations (interfaces + simulators)

Define in `RemitMon.Infrastructure/Adapters`:

```csharp
public interface IBankFeedAdapter        // Jack Henry-style: inbound ACH entries, RTP/FedNow/wire events, return actions
public interface IPaymentOrchestrationAdapter // Modern Treasury-style: payment orders, counterparties, RfP, webhooks
public interface IAccountingAdapter      // QuickBooks-style: bills, invoices, vendors, webhooks
```

For each: a `Simulator*` implementation (default) and a `Real*` stub class that throws `NotConfiguredException` with a clear message. Configuration key `Integrations:Mode = Simulator | Live`.

Webhook endpoints (HMAC signature validation; simulator signs with a dev secret):
- `POST /webhooks/accounting/{provider}` — events: `bill.created`, `bill.updated`, `invoice.created`, `vendor.created`, `vendor.updated`, `vendor.bank_details_changed`
- `POST /webhooks/bank/{provider}` — events: `ach.entry.received` (debit/credit), `rtp.rfp.received`, `rtp.payment.received`, `fednow.rfp.received`, `wire.received`, `payment.initiated`
- Idempotency: require `Idempotency-Key` or event id; dedupe in DB (unique constraint).
- Ingest path: webhook → validate → persist raw → enqueue → worker evaluates → persist `Decision` → execute `Action` via adapter (simulator logs it) → notify.

Accounting webhooks create or update `ExpectedObligation` and `Counterparty` (`source = Accounting`, `status = Pending` until approved). Emit signals for `NewPayeeRisk` rules. Provide a notification when a bill is created ("awareness of invoice creation or bill receipt") to the Business view.

---

## 8. Backend API (REST, versioned `/api/v1`, OpenAPI generated and checked in CI)

Auth roles: `BusinessAdmin`, `BusinessApprover`, `BankAdmin`, `PlatformAdmin`.

- Accounts: `GET /accounts`, `GET /accounts/{id}`, `PATCH /accounts/{id}/mode` (Monitor/Enforce; Enforce requires BusinessAdmin + audit)
- Policies: `GET/POST /accounts/{id}/policies`, `POST /policies/{id}/rules`, `PUT /rules/{id}` (Draft only), `POST /policies/{id}/submit`, `POST /policies/{id}/approve` (different user than author), `POST /policies/{id}/activate`, `GET /policies/{id}/diff/{otherId}`
- Policy simulation: `POST /policies/{id}/test` (run a payload against a draft), `POST /policies/{id}/backtest` (replay last N days of events, return would-have-been decisions)
- Counterparties: CRUD, `POST /counterparties/{id}/approve|block`, `GET /counterparties/{id}/changes`
- Obligations: `GET /obligations`, `POST /obligations` (manual), `GET /obligations/{id}`
- Activity: `GET /events` (filters: rail, initiation, outcome, counterparty, date), `GET /events/{id}` (full explanation: rule hits, matched obligation, actions)
- Approvals: `GET /approvals`, `POST /approvals/{id}/approve`, `POST /approvals/{id}/reject` (note required)
- Actions: `GET /actions`
- Audit: `GET /audit`, `GET /audit/verify` (re-computes hash chain)
- Bank view: `GET /bank/overview` (customers, protected accounts, blocked totals, approval SLAs), `GET /bank/customers/{id}`
- Simulator (PlatformAdmin, disabled in Live mode): `POST /sim/scenarios/{name}/run`, `POST /sim/events` (custom), `POST /sim/reset`, `GET /sim/scenarios`
- Health: `/health/live`, `/health/ready`

Standards: ProblemDetails errors, FluentValidation, pagination (`?page&pageSize`), correlation id header, rate limiting on webhooks.

---

## 9. Frontend (`/web`)

Style: clean, dense, "security console" feel; light/dark; accessible (WCAG AA). Simulated actions are identified as simulated.

### Views and pages

**Business view**
1. **Dashboard** — account protection status (Monitor/Enforce), today's allowed / rejected / escalated counts and dollars, open approvals with countdown timers, expected obligations due soon, blocked-exposure chart (Recharts), recent webhook-driven alerts ("New bill created: Vendor X, $4,200").
2. **Activity** — table of payment events with outcome badges; row drawer with the full rule-hit explanation, matched obligation, raw payload, and actions taken.
3. **Approvals** — queue with context (counterparty, amount, matched/unmatched obligation, risk signals, deadline). Approve / Reject with required note. Keyboard shortcuts.
4. **Policies** — hierarchical editor (Account → Counterparty → Payment type → Transaction override). Rule builder forms per rule type, validation, inline "test a payment" panel, backtest results, version history with diff, four-eyes approval flow.
5. **Counterparties** — list, status, ACH company IDs, bank-detail change history, approve/block.
6. **Obligations** — expected obligations from accounting with match status.
7. **Integrations** — status of accounting/bank/orchestration adapters (simulated), webhook log.
8. **Audit** — searchable audit log with "Verify chain" button.

**Bank view**
9. **Bank Overview** — portfolio of business customers, protected accounts, blocked dollars, approvals by SLA, rail mix, mode adoption. Read-only drill-in per customer. Ability to set bank-level **Locked** baseline rules and `FailSafeOutcome` defaults.

**Admin / Simulator view**
10. **Scenario runner** — buttons for each scenario (Section 11), live event stream, reset.

Frontend rules: generate a typed API client from the OpenAPI spec (`openapi-typescript`); TanStack Query for data; polling every 5s on Dashboard/Approvals (SignalR optional later); no business logic in the browser except display formatting.

---

## 10. Non-functional requirements

- **Latency**: engine evaluation p95 < 50 ms in-process; end-to-end ingest → decision p95 < 500 ms in the simulator. Record `latency_ms` on every `Decision`.
- **Determinism**: golden-file tests: fixed policy snapshot + fixed context → exact expected `EvaluationResult` JSON.
- **Idempotency** on all webhook handlers and action execution.
- **Security**: HMAC webhook validation, least-privilege managed identity, no secrets in repo, PII minimization (store masked account numbers and fingerprints, never full numbers), audit on every mutation, CORS locked to the static web app origin.
- **Resilience**: retry with exponential backoff and a dead-letter queue; fail-safe outcomes per Section 5.3 when the engine or a dependency fails.
- **Multi-tenancy**: every query filtered by bank/business via a global EF query filter derived from the authenticated user.

---

## 11. Simulator scenarios (seed fictional data)

Seed 2 banks, 3 business customers, 4 accounts, 12 counterparties, 30 days of history. Implement named scenarios that produce visible UI results:

1. **`ach-pull-matched`** — QuickBooks `bill.created` for an approved vendor, then a matching ACH debit → Allow, obligation Matched.
2. **`ach-pull-unknown-originator`** — debit from unknown company ID, no obligation → Reject (Enforce) / would-reject (Monitor).
3. **`ach-pull-over-limit`** — known vendor, amount above per-transaction cap → Escalate; approver approves/rejects in UI.
4. **`new-vendor-new-bill-fast`** — vendor created and bill created within minutes, then a pull → flagged by `NewPayeeRisk`, Escalate.
5. **`vendor-bank-details-changed`** — `vendor.bank_details_changed` followed by an RfP from that vendor → Escalate with change-log context.
6. **`rtp-rfp-within-policy`** — RfP matching an open obligation → auto-accepted (simulated).
7. **`rtp-rfp-out-of-policy`** — RfP with no obligation → declined; RfP with expiry → auto-decline on timeout.
8. **`velocity-burst`** — 12 small RTP/FedNow payments in 5 minutes by an "agent" → velocity rule trips, subsequent items Reject.
9. **`after-hours-wire`** — wire outside allowed window → Escalate.
10. **`recurring-standing-auth`** — utility pull with no bill but within standing authorization → Allow.
11. **`monitor-vs-enforce`** — same traffic evaluated in both modes to show the difference.
12. **`engine-failure-failsafe`** — inject engine exception → fail-safe outcome applied and audited.

---

## 12. Azure infrastructure (`/infra`, Bicep)

Resources per environment (`dev`, `prod`): Resource Group, Log Analytics + Application Insights, Key Vault, User-assigned Managed Identity, Azure Database for PostgreSQL Flexible Server (private access where feasible), Service Bus namespace + queues (`bank-events`, `accounting-events`, `actions`, dead-letter), Azure Container Apps environment + API app (min 1 replica), Azure Static Web App for `/web`, Entra app registrations documented in `docs/ARCHITECTURE.md`.

GitHub Actions: `ci.yml` (restore, build, test, OpenAPI spec generation and diff check, web lint/test/build), `deploy-api.yml` (container build → ACR → Container Apps via OIDC federated credentials), `deploy-web.yml` (Static Web Apps). Run EF Core migrations as a deploy step using a migration bundle.

---

## 13. Build order for Copilot (do in sequence, commit after each)

1. Solution scaffold, devcontainer, CI skeleton, README.
2. `RemitMon.Domain` entities and enums; EF Core DbContext, migrations, seed data.
3. `RemitMon.Engine`: rule types, resolution semantics, matching service, banking calendar interface. **Write the xUnit tests alongside**, including golden files and a test per scenario in Section 11.
4. API host: auth (dev + Entra), tenancy filter, controllers for accounts, policies, counterparties, obligations, events, approvals, audit (hash chain + verify).
5. Ingestion pipeline: webhook endpoints, Service Bus abstraction (in-memory + Azure), worker that evaluates and records decisions and actions.
6. Adapters: simulator implementations, real stubs, scenario runner endpoints.
7. Frontend: shell, auth, typed client, then pages in this order: Dashboard, Activity, Approvals, Policies, Counterparties, Obligations, Bank Overview, Simulator, Audit, Integrations.
8. Playwright smoke tests (ach-pull-over-limit approve flow; policy draft → approve → activate; monitor → enforce switch).
9. Bicep infra and deploy workflows.
10. `docs/ARCHITECTURE.md` (include a sequence diagram per rail type) and `docs/DECISIONS.md`.

---

## 14. Acceptance criteria

- `dotnet test` passes, with engine coverage ≥ 90% on `RemitMon.Engine`.
- Every simulator scenario in Section 11 produces the expected decision, action record, audit entries, and UI state.
- Activating a policy requires a second user; the author cannot self-approve.
- The audit chain verifies; tampering with a row in a test causes `/audit/verify` to fail.
- No code path returns `Allow` when the engine throws or required data is missing.
- Switching an account from Monitor to Enforce changes actions taken but not decisions computed for identical input.
- No simulated action is presented as a real bank action.
- `docs/openapi.json` is generated and CI fails on unreviewed drift.

---

## 15. Explicit assumptions (verify before any live use)

- ACH return and reversal windows, RfP expiry behavior, and which actions each bank/core exposes vary by institution and are held in configuration, not code.
- Returning or rejecting ACH entries requires bank-side return-processing access; the prototype simulates this through `IBankFeedAdapter`.
- Real Jack Henry, Modern Treasury, and QuickBooks integrations are out of scope for the prototype; only interfaces, stubs, and simulators are built.
- Coverage of unsolicited credit pushes depends on bank enforcement of RM as a required path; the prototype models this as the `Monitor`/`Enforce` account mode and policy scope.
