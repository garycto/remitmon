# RemitMon Architecture

## System Overview

RemitMon is a bank-distributed account protection platform built on a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────┐
│          Frontend (React + TypeScript)              │
│        ├─ Dashboard (activity overview)             │
│        ├─ Accounts (account management)             │
│        ├─ Policies (rule builder)                   │
│        └─ Exceptions (approval queue)               │
└──────────────────┬──────────────────────────────────┘
                   │ HTTP/REST (OpenAPI)
┌──────────────────▼──────────────────────────────────┐
│          API Layer (Minimal APIs)                   │
│        ├─ Account Endpoints                         │
│        ├─ Policy Endpoints                          │
│        ├─ Counterparty Endpoints                    │
│        ├─ Exception Endpoints                       │
│        └─ Health/Status Endpoints                   │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│          Domain Logic Layer                         │
│        ├─ Policy Engine (pure evaluation)           │
│        ├─ Domain Entities (rich models)             │
│        └─ Business Rules                            │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│       Infrastructure Layer                          │
│        ├─ EF Core + PostgreSQL                      │
│        ├─ Adapters (Bank, Accounting, Payment)      │
│        ├─ Service Bus (queues)                      │
│        └─ Webhooks                                  │
└─────────────────────────────────────────────────────┘
```

## Layer Breakdown

### 1. Frontend Layer (`/web`)

**Technology Stack:**
- React 18 + TypeScript
- Vite (build tool)
- TanStack Query (data fetching)
- Tailwind CSS (styling)
- Recharts (data visualization)
- React Router (navigation)

**Features:**
- Responsive design with dark mode
- Real-time data syncing via polling
- Form-based policy builder
- Approval workflow UI
- Dashboard with charts and metrics

**Deployment:**
- Azure Static Web Apps
- Automatic deployment from GitHub

### 2. API Layer (`/src/RemitMon.Api`)

**Architecture:**
- Minimal APIs (not MVC controllers)
- Grouped endpoints by resource
- Swagger/OpenAPI documentation
- ProblemDetails error handling
- Serilog structured logging
- CORS configured for frontend

**Endpoints:**
```
GET/POST   /api/v1/accounts
GET/PATCH  /api/v1/accounts/{id}
PATCH      /api/v1/accounts/{id}/mode

GET/POST   /api/v1/policies
POST       /api/v1/policies/{id}/rules
POST       /api/v1/policies/{id}/activate
POST       /api/v1/policies/{id}/test

GET/POST   /api/v1/counterparties
POST       /api/v1/counterparties/{id}/approve
POST       /api/v1/counterparties/{id}/block
GET        /api/v1/counterparties/{id}/changes

GET/POST   /api/v1/exceptions
POST       /api/v1/exceptions/{id}/approve
POST       /api/v1/exceptions/{id}/reject

GET        /health
```

**Hosting:**
- Azure Container Apps
- Auto-scaling (1-3 replicas)
- Environment-based configuration
- Managed Identity for Azure access

### 3. Domain Logic Layer

#### 3.1 Engine (`/src/RemitMon.Engine`)

**Purpose:**
- Pure policy evaluation engine
- Zero external dependencies
- Deterministic results

**Rule Types:**
1. **CounterpartyAllowlist** — Whitelist/blocklist known payees
2. **PaymentTypeAllowed** — Filter by rail and initiation type
3. **MaxAmountPerTransaction** — Per-transaction ceiling
4. **MaxAmountPerCounterparty** — Cumulative exposure per payee
5. **CumulativeExposure** — Rolling window exposure (day/week/month)
6. **Velocity** — Transaction rate limiting
7. **TimeWindow** — Restrict to business hours or time ranges
8. **Purpose** — Match against expected obligations
9. **NewPayeeRisk** — Flag new counterparties or bank detail changes
10. **Authorization** — Approval thresholds

**Evaluation Algorithm:**
```
1. Collect all enabled rules whose scope applies
2. Order by scope specificity: Transaction > Counterparty > PaymentType > Account
3. Evaluate each rule (no short-circuit)
4. Combine results:
   - Any Reject → Reject
   - Else any Escalate → Escalate
   - Else → Allow
5. Store decision with rule hits and latency_ms
```

**Input:**
```csharp
PolicyContext {
  PaymentEvent event,
  Counterparty? counterparty,
  ExpectedObligation? obligation,
  RunningAggregates aggregates,
  DateTime evaluationTime
}
```

**Output:**
```csharp
EvaluationResult {
  DecisionOutcome outcome,
  List<RuleHit> ruleHits,
  int policyVersion,
  int latencyMs
}
```

#### 3.2 Domain Model (`/src/RemitMon.Domain`)

**15 Entity Classes:**

**Tenancy & Structure:**
- `Bank` — Banking institution
- `BusinessCustomer` — Tenant (business customer)
- `Account` — Protected account (Monitor/Enforce mode)
- `User` — Multi-tenant user with role

**Counterparties:**
- `Counterparty` — Payee/vendor (Pending/Approved/Blocked)
- `CounterpartyChangeLog` — Change audit trail

**Policies:**
- `Policy` — Versioned rule set (Draft/Active/Archived)
- `PolicyRule` — Individual rules with parameters

**Activity & Decisions:**
- `PaymentEvent` — Inbound/outbound transactions
- `ExpectedObligation` — Accounting integration events
- `Decision` — Policy evaluation outcome
- `Exception` — Escalations requiring approval
- `ActionRecord` — Recourse actions (Reject, Return, Reverse, etc.)
- `AuditEntry` — Hash-chained audit log

**13 Enumerations:**
- AccountMode, AccountStatus
- UserRole
- CounterpartyStatus, CounterpartySource
- PolicyStatus
- Rail, PaymentDirection, PaymentInitiation
- DecisionOutcome, ActionType, ActionStatus
- ExceptionStatus, ExpectedObligationStatus/Source
- BusinessCustomerStatus

### 4. Infrastructure Layer (`/src/RemitMon.Infrastructure`)

#### 4.1 Database (EF Core + PostgreSQL)

**Schema:**
- Snake_case table naming convention
- UUID primary keys (gen_random_uuid())
- Automatic migrations on startup
- Foreign key relationships
- Seed data with realistic test data

**Relationships:**
```
Bank (1) ──┬─ BusinessCustomer (n)
           ├─ Account (n)
           └─ User (n)

BusinessCustomer (1) ──┬─ Account (n)
                       ├─ User (n)
                       ├─ Counterparty (n)
                       └─ ExpectedObligation (n)

Account (1) ──┬─ Policy (n)
              └─ PaymentEvent (n)

Policy (1) ──┬─ PolicyRule (n)
             └─ Decision (n)

PaymentEvent (1) ──┬─ Counterparty
                   ├─ Decision (n)
                   └─ ExpectedObligation

Decision (1) ──┬─ Exception (n)
               └─ ActionRecord (n)

Counterparty (1) ─── CounterpartyChangeLog (n)
```

**Seed Data:**
- 2 banks
- 2 business customers
- 3 accounts
- 4 users (admin, approver, bank admin, platform admin)
- 2 counterparties
- 1 policy with 2 sample rules

#### 4.2 Adapters (Interfaces + Simulators)

**Interfaces:**
```csharp
public interface IBankFeedAdapter { }        // ACH, RTP, FedNow, Wire
public interface IPaymentOrchestrationAdapter { } // Payment orders, RfP
public interface IAccountingAdapter { }     // Bills, invoices, vendors
```

**Simulators:**
- SimulatorBankFeedAdapter
- SimulatorPaymentOrchestrationAdapter
- SimulatorAccountingAdapter

**Configuration:**
- `Integrations:Mode = Simulator | Live`

#### 4.3 Webhooks

**Endpoints:**
```
POST /webhooks/accounting/{provider}
POST /webhooks/bank/{provider}
```

**Events:**
- Accounting: `bill.created`, `invoice.created`, `vendor.created`, `vendor.bank_details_changed`
- Bank: `ach.entry.received`, `rtp.rfp.received`, `wire.received`

**Validation:**
- HMAC signature verification
- Idempotency via event deduplication

## Data Flow

### Payment Evaluation Flow

```
1. Webhook ingress
   └─ Bank/Accounting adapter sends event
      └─ Validate HMAC signature
         └─ Persist raw payload
            └─ Enqueue to Service Bus

2. Event processing
   └─ Worker dequeues event
      └─ Create PaymentEvent entity
         └─ Resolve counterparty (by ACH company ID, name)
            └─ Match to ExpectedObligation (fuzzy matching)
               └─ Query PolicyEngine.Evaluate()
                  └─ Store Decision + RuleHits

3. Decision handling
   └─ If Escalate
      └─ Create Exception (open, assigned, with deadline)
         └─ Notify approver
            └─ Await manual approval
   └─ Else if Enforce mode
      └─ Execute Action via adapter
         └─ Reject / Return / Reverse / etc.
         └─ Store ActionRecord (status, details)

4. Audit trail
   └─ Hash-chain all mutations
      └─ Store AuditEntry with SHA-256 hash
         └─ Enable integrity verification
```

### Decision Resolution Flow

```
Decision Outcome:
  ├─ ALLOW
  │  └─ Monitor mode: log decision, no action
  │  └─ Enforce mode: permit payment, log decision
  │
  ├─ REJECT
  │  └─ ACH pull pre-posting: return as unauthorized
  │  └─ ACH credit: reverse (error cases)
  │  └─ RfP: decline immediately
  │  └─ Wire: reject at initiation
  │
  └─ ESCALATE
     ├─ RfP: hold until expiry, auto-decline on timeout
     ├─ ACH pull: hold until pre-posting cutoff, auto-reject at cutoff
     ├─ Push rails: reject with resubmit flag (no indefinite hold)
     └─ Create Exception → human approval/rejection
```

## Deployment Architecture

### Local Development

```
docker-compose.yml
  └─ PostgreSQL 16

dotnet run
  └─ RemitMon.Api (http://localhost:5199)
     └─ Swagger UI (http://localhost:5199/swagger)

npm run dev (in /web)
  └─ Frontend (http://localhost:5173)
     └─ Proxy to API
```

### Azure Deployment (Bicep IaC)

```
Resource Group
  ├─ PostgreSQL Flexible Server
  │  ├─ Database: remitmon
  │  ├─ User: remitmon_admin
  │  └─ Automated backups (7 days)
  │
  ├─ Container Apps Environment
  │  └─ Container App (RemitMon.Api)
  │     ├─ Image: ACR (auto-deployed via CI/CD)
  │     ├─ Port: 5000
  │     ├─ Replicas: 1-3 (auto-scale)
  │     └─ Environment variables: DB connection, settings
  │
  └─ Static Web App
     ├─ GitHub integration
     ├─ Branch: main
     ├─ Build: npm run build
     ├─ Output: /web/dist
     └─ Auto-deployed on push
```

**Bicep Modules:**
- `main.bicep` — Orchestrator
- `modules/postgres.bicep` — Database
- `modules/container-app.bicep` — API
- `modules/static-web-app.bicep` — Frontend

## Security Architecture

### Authentication

**Development:**
- Seeded users (admin, approver, bank admin, platform admin)
- Mode: `Auth:Mode = Development`

**Production:**
- Microsoft Entra ID integration
- JWT bearer tokens
- Managed identity for Azure resources

### Authorization

**Roles:**
- `BusinessAdmin` — Full account control
- `BusinessApprover` — Approve exceptions and policies
- `BankAdmin` — Bank-level oversight
- `PlatformAdmin` — System administration

### Data Protection

- PII minimization (masked account numbers, hashed tax IDs)
- HTTPS only (dev exception: localhost)
- Secrets in Key Vault (not repo)
- CORS locked to static web app origin

### Audit Trail

- Hash-chained audit log (SHA-256)
- Every mutation captured
- Integrity verification endpoint
- Previous hash included (blockchain-style)

## Observability

### Logging

- **Serilog**: Structured JSON logs
- **Console**: Real-time during development
- **Rolling File**: `logs/remitmon-{date}.txt`
- **Application Insights**: Production (ready for integration)

### Metrics

- **Latency**: Policy evaluation p95 < 50ms (recorded in Decision)
- **Throughput**: Decisions per second
- **Error rates**: Engine failures, adapter timeouts
- **Queue depth**: Service Bus backlog

### Tracing

- Correlation ID on every request
- Request/response logging via Serilog middleware
- Ready for Application Insights integration

## Testing Strategy

### Unit Tests

- **Engine**: 10 test cases covering all rule types
- **Domain**: Entity validation and relationships
- **API**: Endpoint contracts and error handling

### Integration Tests

- Database seed data verification
- End-to-end policy evaluation with real data
- Webhook ingress and processing

### E2E Tests (Planned)

- Playwright: 3 smoke flows
  - Business user logs in, views dashboard, creates account
  - Policy creation and rule configuration
  - Exception approval workflow

## Development Workflow

1. Create feature branch off `main`
2. Make changes (follow C# conventions)
3. Run tests locally: `dotnet test`
4. Build solution: `dotnet build`
5. Commit with descriptive message
6. Create pull request with description

**CI/CD:**
- GitHub Actions
- Runs on every push/PR: build, test, OpenAPI check
- Deploy on merge to main

## Technical Decisions

See `docs/DECISIONS.md` for detailed rationale on:
- Minimal APIs vs controllers
- Snake_case table naming
- UUID primary keys
- Serilog for logging
- EF Core for data access
- Pure engine design

## Future Enhancements

- Real adapter implementations (Jack Henry, Modern Treasury, QuickBooks)
- Advanced matching engine (fuzzy accounting matching)
- Real-time notifications (SignalR)
- Rate limiting and backpressure
- Multi-tenant query filters at DbContext level
- Standing authorizations (recurring, pre-approved payments)
- Risk scoring signals (new payee, bank detail changes)
- Webhook replay/dead-letter queue handling
- OpenAPI client generation (openapi-typescript)
