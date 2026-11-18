# RemitMon — Account Protection Platform

RemitMon is a bank-distributed account protection platform that places configurable, deterministic financial guardrails around business bank accounts. It evaluates ACH, RTP, FedNow, and wire activity against customer-defined policies and expected obligations from accounting software.

## 🚀 Quick Start

### Authentication & Database

RemitMon uses **Supabase** (PostgreSQL) and **GitHub OAuth** for authentication.

**Before running:**
1. Create a Supabase project at https://supabase.com
2. Create a GitHub OAuth App at https://github.com/settings/developers
3. Configure connection strings (see [SUPABASE_SETUP.md](SUPABASE_SETUP.md))

### Local Development

**Prerequisites:**
- .NET 8 SDK
- Node.js 20+
- Supabase account
- GitHub OAuth App

**Setup:**

```bash
# 1. Configure appsettings.json with Supabase credentials
# Edit: src/RemitMon.Api/appsettings.json
# Add your Supabase connection string and GitHub OAuth credentials

# 2. Restore dependencies
dotnet restore

# 3. Build solution
dotnet build

# 4. Run API (Terminal 1)
dotnet run --project src/RemitMon.Api
# API available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
```

### Frontend Development

```bash
# Terminal 2
cd web
npm install
npm run dev
```

Frontend runs at `http://localhost:5173`

### Authentication Flow

1. Click **"Login with GitHub"** in the UI
2. Redirects to GitHub authorization
3. After approval, you're authenticated
4. Session stored securely via ASP.NET Core cookies

## Project Structure

```
/remitmon
  /.devcontainer          # VS Code dev container config
  /.github/workflows      # CI/CD pipelines
  /docs                   # Architecture & decision docs
  /infra                  # Bicep IaC for Azure deployment
  /src
    /RemitMon.Api         # ASP.NET Core host, controllers
    /RemitMon.Domain      # Entities, enums, value objects
    /RemitMon.Engine      # Policy evaluation engine (pure logic)
    /RemitMon.Engine.Tests # Engine tests
    /RemitMon.Infrastructure # EF Core, adapters, database
  /web                    # React + TypeScript frontend
  RemitMon.sln            # Solution file
  README.md               # This file
  SUPABASE_SETUP.md       # Detailed Supabase setup guide
```

## Architecture Layers

### 1. Domain Layer (`RemitMon.Domain`)
- **15 Entities**: Bank, BusinessCustomer, Account, User, Counterparty, Policy, PolicyRule, PaymentEvent, ExpectedObligation, Decision, Exception, ActionRecord, AuditEntry
- **13 Enums**: For type-safe status values across all entities
- Pure domain model with no external dependencies

### 2. Engine Layer (`RemitMon.Engine`)
- Policy evaluation engine with 10 rule types
- Pure functions: no I/O, no randomness, fully testable
- Deterministic: same input + same policy = same decision always
- Returns evaluation result with rule hits and latency metrics

### 3. Infrastructure Layer (`RemitMon.Infrastructure`)
- EF Core 8 with PostgreSQL (Supabase)
- Snake_case table naming convention
- Database migrations on startup
- Seed data with 2 banks, 2 customers, 3 accounts, 4 users

### 4. API Layer (`RemitMon.Api`)
- Minimal APIs with OpenAPI/Swagger documentation
- GitHub OAuth authentication
- REST endpoints for accounts, policies, counterparties, exceptions
- Serilog structured logging
- CORS configured for frontend

## REST API Endpoints

### Authentication
- `GET /auth/login` — Redirect to GitHub login
- `GET /auth/logout` — Clear session and logout
- `GET /auth/me` — Get current user info

### Accounts
- `GET /api/v1/accounts` — List all accounts
- `GET /api/v1/accounts/{id}` — Get account details
- `POST /api/v1/accounts` — Create new account
- `PATCH /api/v1/accounts/{id}/mode` — Update Monitor/Enforce mode

### Policies
- `GET /api/v1/policies/account/{accountId}` — List policies for account
- `GET /api/v1/policies/{id}` — Get policy details
- `POST /api/v1/policies` — Create new policy (Draft)
- `POST /api/v1/policies/{id}/rules` — Add rule to policy
- `POST /api/v1/policies/{id}/activate` — Activate policy (requires approval)
- `POST /api/v1/policies/{id}/test` — Test policy with payment event

### Counterparties
- `GET /api/v1/counterparties/business/{businessId}` — List counterparties
- `GET /api/v1/counterparties/{id}` — Get counterparty details
- `POST /api/v1/counterparties` — Create new counterparty
- `POST /api/v1/counterparties/{id}/approve` — Approve counterparty
- `POST /api/v1/counterparties/{id}/block` — Block counterparty
- `GET /api/v1/counterparties/{id}/changes` — Get change history

### Exceptions
- `GET /api/v1/exceptions` — List all exceptions
- `GET /api/v1/exceptions/{id}` — Get exception details
- `POST /api/v1/exceptions/{id}/approve` — Approve exception
- `POST /api/v1/exceptions/{id}/reject` — Reject exception

### Health
- `GET /health` — Health check

## Configuration

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;User Id=remitmon;Password=remitmon_dev;Database=remitmon;"
  },
  "Auth": {
    "Mode": "Development"
  },
  "Integrations": {
    "Mode": "Simulator"
  }
}
```

**Environment Variables:**
- `ConnectionStrings__DefaultConnection` — Database connection string
- `ASPNETCORE_ENVIRONMENT` — dev/staging/prod
- `Integrations__Mode` — Simulator or Live

## Domain Model

### Core Entities

**Tenancy & Structure:**
- `Bank` — Bank institution
- `BusinessCustomer` — Tenant (business customer of a bank)
- `Account` — Protected bank account with mode (Monitor/Enforce)
- `User` — Multi-tenant users with roles

**Counterparties:**
- `Counterparty` — Payee/vendor with approval status
- `CounterpartyChangeLog` — Change audit trail

**Policy Management:**
- `Policy` — Versioned rule set (Draft → Active → Archived)
- `PolicyRule` — 10 rule types with parameters

**Activity:**
- `PaymentEvent` — Inbound/outbound transactions
- `ExpectedObligation` — Accounting integration events
- `Decision` — Policy evaluation outcome
- `Exception` — Escalations requiring human approval
- `ActionRecord` — Recourse actions (Reject, Reverse, etc.)
- `AuditEntry` — Hash-chained audit log

## Policy Rules (10 Types)

1. **CounterpartyAllowlist** — Whitelist/blocklist known counterparties
2. **PaymentTypeAllowed** — Filter by rail (ACH/RTP/Wire) and initiation (Push/Pull/RfP)
3. **MaxAmountPerTransaction** — Cap per-transaction amount
4. **MaxAmountPerCounterparty** — Cumulative exposure per counterparty
5. **CumulativeExposure** — Rolling window exposure (day/week/month)
6. **Velocity** — Transaction rate limiting (count/amount per time window)
7. **TimeWindow** — Restrict to business hours or specific time ranges
8. **Purpose** — Match against expected obligations from accounting
9. **NewPayeeRisk** — Flag new counterparties or bank detail changes
10. **Authorization** — Approval thresholds (auto-approve below, escalate above)

## Decision Engine

**Evaluation:**
1. Collect all enabled rules whose scope applies
2. Evaluate all rules (no short-circuit)
3. Combine outcomes:
   - Any **Reject** hit → **Reject**
   - Any **Escalate** hit → **Escalate**
   - Else → **Allow**
4. Store decision with rule hits and latency

**Modes:**
- **Monitor**: Compute decision, apply no action, log for audit
- **Enforce**: Apply action (Reject, Return, Reverse, etc.)

**Fail-Safe:**
- Engine errors → Reject (for pulls/RfP) or Escalate per account setting
- Never silently Allow on error

## Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test src/RemitMon.Engine.Tests

# With coverage
dotnet test /p:CollectCoverage=true
```

## Deployment

### Local Docker
```bash
docker-compose up -d
dotnet run --project src/RemitMon.Api
```

### Azure Deployment (via Bicep)
```bash
az group create --name remitmon-rg --location eastus

az deployment group create \
  --resource-group remitmon-rg \
  --template-file infra/main.bicep \
  --parameters infra/parameters.dev.json
```

### CI/CD Pipelines
- **ci.yml** — Runs on every push/PR: build, test, OpenAPI check
- **deploy-api.yml** — Deploy API container to Azure Container Apps
- **deploy-web.yml** — Deploy frontend to Azure Static Web Apps

## Development Decisions

See [`docs/DECISIONS.md`](docs/DECISIONS.md) for technical decisions and rationale.

### Key Decisions:
- **Minimal APIs** over controllers for cleaner separation
- **Snake_case** tables for PostgreSQL convention
- **UUID primary keys** for distributed systems
- **Serilog** for structured logging
- **EF Core** for type-safe database access
- **Pure engine** for determinism and testability

## Security

- **HTTPS** only (dev uses localhost exception)
- **HMAC webhook validation** (not yet implemented)
- **JWT bearer auth** via Microsoft Entra ID (dev mode: seeded users)
- **Least-privilege managed identity** for Azure resources
- **PII minimization**: masked account numbers, hashed tax IDs
- **Audit trail**: every mutation logged and hash-chained

## Observability

- **Serilog** structured logging to console + rolling file
- **OpenTelemetry** ready for Application Insights integration
- **Request correlation ID** on all HTTP requests
- **Latency metrics** on every policy evaluation
- **Audit hash chain** for integrity verification

## Contributing

1. Create a feature branch off `main`
2. Make changes (follow C# conventions)
3. Run tests locally: `dotnet test`
4. Build solution: `dotnet build`
5. Commit with message describing change
6. Create pull request with description

## Roadmap

- [ ] Webhook integration (bank, accounting adapters)
- [ ] Azure Service Bus queue integration
- [ ] React frontend build-out
- [ ] Real adapter stubs (Jack Henry, Modern Treasury, QuickBooks)
- [ ] E2E tests (Playwright)
- [ ] Advanced matching engine (accounting fuzzy matching)
- [ ] Real-time notifications (SignalR)
- [ ] Rate limiting and backpressure
- [ ] API versioning strategy

## License

Proprietary — RemitMon platform

## Support

For issues and questions, contact the RemitMon team.
