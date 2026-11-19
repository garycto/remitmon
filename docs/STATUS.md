# Implementation status

Audit of Section 13, steps 1–6, in [REMITMON.md](REMITMON.md), with presentation context from [INFO_ARCHITECTURE.md](INFO_ARCHITECTURE.md). Status reflects the current working tree, not only committed files.

| Step | Status | Evidence |
|---|---|---|
| 1. Scaffold, dev container, CI, README | Done | [Solution](../RemitMon.sln), [dev container](../.devcontainer/devcontainer.json), [CI](../.github/workflows/ci.yml), and [README](README.md) exist. |
| 2. Domain, EF Core, migrations, seed data | Partial | [Domain entities](../src/RemitMon.Domain/Entities), [enums](../src/RemitMon.Domain/Enums), [DbContext](../src/RemitMon.Infrastructure/RemitMonDbContext.cs), [migrations](../src/RemitMon.Infrastructure/Migrations), and [demo seeder](../src/RemitMon.Infrastructure/Seeding/DemoDataSeeder.cs) exist. The payment event has no external-event ID or unique idempotency constraint; account default/fail-safe outcomes described in the specification are also absent. Startup uses `EnsureCreated`, not automatic EF migrations ([Program.cs](../src/RemitMon.Api/Program.cs)). |
| 3. Policy engine, matching, calendar, tests | Missing | [Engine](../src/RemitMon.Engine/Class1.cs) is an empty class; [engine test](../src/RemitMon.Engine.Tests/UnitTest1.cs) is empty. Rule evaluation, matching, banking calendar, and scenario tests are absent. The [policy test endpoint](../src/RemitMon.Api/Endpoints/PolicyEndpoints.cs) returns a fixed message instead of evaluating. |
| 4. API host, authentication, tenancy, endpoints | Partial | [Program.cs](../src/RemitMon.Api/Program.cs) configures cookie authentication and registers endpoint groups. [Account](../src/RemitMon.Api/Endpoints/AccountEndpoints.cs), [policy](../src/RemitMon.Api/Endpoints/PolicyEndpoints.cs), [counterparty](../src/RemitMon.Api/Endpoints/CounterpartyEndpoints.cs), [approval](../src/RemitMon.Api/Endpoints/ApprovalEndpoints.cs), and [dashboard](../src/RemitMon.Api/Endpoints/DashboardEndpoints.cs) endpoints exist. Entra authentication, tenant query filtering, and the specified obligation, event, and audit endpoints are absent. |
| 5. Webhook ingestion, queue, worker, decisions and actions | Missing | No webhook endpoints, queue abstraction, queue implementation, or ingestion worker exist in the source projects. [Program.cs](../src/RemitMon.Api/Program.cs) registers no such components. |
| 6. Adapters, simulators, real stubs, scenario runner | Missing | No adapter interfaces or implementations, including `IPaymentOrchestrationAdapter` and `SimulatorPaymentOrchestrationAdapter`, or scenario runner endpoints exist in the source projects. |

## Required components

| Component | Status | Evidence |
|---|---|---|
| `PaymentEvent` | Exists | [PaymentEvent.cs](../src/RemitMon.Domain/Entities/PaymentEvent.cs) |
| `Counterparty` | Exists | [Counterparty.cs](../src/RemitMon.Domain/Entities/Counterparty.cs) |
| `ExpectedObligation` | Exists | [ExpectedObligation.cs](../src/RemitMon.Domain/Entities/ExpectedObligation.cs) |
| `Decision` | Exists | [Decision.cs](../src/RemitMon.Domain/Entities/Decision.cs) |
| EF Core DbContext and migrations | Exist | [RemitMonDbContext.cs](../src/RemitMon.Infrastructure/RemitMonDbContext.cs) and [Migrations](../src/RemitMon.Infrastructure/Migrations) |
| `RemitMon.Engine` | Project exists; implementation missing | [Class1.cs](../src/RemitMon.Engine/Class1.cs) |
| Webhook endpoints | Missing | No webhook endpoint registrations in [Program.cs](../src/RemitMon.Api/Program.cs) or endpoint implementations in [Endpoints](../src/RemitMon.Api/Endpoints). |
| Ingest queue abstraction and worker | Missing | No queue or hosted worker in [Infrastructure](../src/RemitMon.Infrastructure) or [API](../src/RemitMon.Api). |
| `IPaymentOrchestrationAdapter` | Missing | No adapter interface in [Infrastructure](../src/RemitMon.Infrastructure). |
| `SimulatorPaymentOrchestrationAdapter` | Missing | No simulator adapter in [Infrastructure](../src/RemitMon.Infrastructure). |
