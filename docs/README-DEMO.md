# RemitMon - Demo Guide

See [PROJECT-OVERVIEW.md](PROJECT-OVERVIEW.md) for the application overview and setup.

A sophisticated financial compliance monitoring system built with .NET 10, designed to detect and prevent fraudulent remittance transactions through intelligent policy-based decision making.

## 🚀 Quick Start

### Prerequisites
- .NET 10 SDK
- Supabase account (or PostgreSQL 14+)
- VS Code with C# Dev Kit

### Local Development

1. **Build the solution**
   ```bash
   dotnet build
   ```

2. **Configure Supabase connection**
   - Create a new Supabase project at https://supabase.com
   - In the Supabase dashboard, copy the IPv4-compatible **Session pooler** host and project-specific username
   - Set `ConnectionStrings__DefaultConnection` in the repository-root `.env.local` using Npgsql key/value format (`Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require`); do not commit this file

3. **Start the API (F5 in VS Code)**
   - API runs on `http://localhost:5199`
   - Database initializes automatically
   - Demo data seeds in development environment
   - Browser opens to interactive API docs

4. **View API Documentation**
   - Navigate to http://localhost:5199/api-docs.html
   - Explore all 22 endpoints with interactive UI
   - Expandable details show parameters and responses

## 📊 Demo Data

The application automatically seeds realistic demo data on first run in development:

### Banks
- **US Bank** (Routing: 111000025)
- **HSBC UK** (Routing: 403000)

### Business Customers
- **Acme Corp USA** (US Bank customer)
- **Global Finance Ltd** (HSBC UK customer)

### Accounts
- **USD Operating Account** (Monitor Mode)
- **EUR International Payments** (Enforce Mode)
- **GBP Treasury Account** (Monitor Mode)

### Policies
1. **High-Value Transaction Alert** (Active)
   - Rule 1: Flag amounts > $50,000 (Escalate)
   - Rule 2: Block amounts > $100,000 (Reject)

2. **OFAC Compliance Check** (Active)
   - Block transfers to sanctioned countries (Iran, North Korea, Syria)

3. **Unusual Activity Detection** (Draft)

### Counterparties
- Partner Bank Switzerland (Approved)
- Emerging Markets Corp (Pending Review)
- Tokyo Financial Partners (Approved)

## 📡 API Endpoints (22 Total)

### Health & Auth
- `GET /health` - Health check
- `GET /auth/me` - Current user
- `GET /auth/logout` - Logout

### Accounts (4 endpoints)
- `GET /api/v1/accounts` - List all accounts
- `POST /api/v1/accounts` - Create account
- `GET /api/v1/accounts/{id}` - Get account details
- `PATCH /api/v1/accounts/{id}/mode` - Toggle Monitor/Enforce mode

### Policies (5 endpoints)
- `GET /api/v1/policies/account/{accountId}` - List policies for account
- `GET /api/v1/policies/{id}` - Get policy details
- `POST /api/v1/policies` - Create policy
- `POST /api/v1/policies/{id}/rules` - Add rule to policy
- `POST /api/v1/policies/{id}/activate` - Activate policy

### Decision Engine (1 endpoint)
- `POST /api/v1/policies/{id}/test` - Test policy with payment event

### Counterparties (5 endpoints)
- `GET /api/v1/counterparties/business/{businessId}` - List counterparties
- `GET /api/v1/counterparties/{id}` - Get counterparty details
- `POST /api/v1/counterparties` - Create counterparty
- `POST /api/v1/counterparties/{id}/approve` - Approve counterparty
- `POST /api/v1/counterparties/{id}/block` - Block counterparty
- `GET /api/v1/counterparties/{id}/changes` - View change history

### Approvals (4 endpoints)
- `GET /api/v1/approvals` - List all approvals
- `GET /api/v1/approvals/{id}` - Get approval details
- `POST /api/v1/approvals/{id}/approve` - Approve request
- `POST /api/v1/approvals/{id}/reject` - Reject request

## 🏗️ Architecture

### Project Structure
```
RemitMon.sln
├── src/RemitMon.Api                 # ASP.NET Core API
├── src/RemitMon.Domain              # Domain models & business logic
├── src/RemitMon.Engine              # Decision engine & rules
├── src/RemitMon.Infrastructure      # EF Core, DbContext, Seeding
├── src/RemitMon.Engine.Tests        # Unit tests
└── web/                             # React + Vite frontend
```

### Technology Stack
- **Backend**: .NET 10, ASP.NET Core, Entity Framework Core
- **Database**: PostgreSQL (Supabase)
- **Authentication**: GitHub OAuth (via Supabase Auth)
- **API Documentation**: OpenAPI 3.1.1 with custom ReDoc viewer
- **Frontend**: React 18, Vite, TypeScript, Tailwind CSS
- **CI/CD**: GitHub Actions

## 🧪 Testing

### Run Unit Tests
```bash
dotnet test
```

### Test API Endpoints
```bash
# Health check
curl http://localhost:5199/health

# List accounts
curl http://localhost:5199/api/v1/accounts

# View API spec
curl http://localhost:5199/openapi/v1.json | jq
```

## 🔄 Development Workflow

1. **F5 to Debug**: Launches API with hot reload, opens browser
2. **API Documentation**: Available at http://localhost:5199/api-docs.html
3. **Demo Data**: Automatically populated on first run
4. **Git Integration**: Configured with GitHub Actions CI/CD

## 📚 Key Features

✅ **Policy-Based Decision Making** - Define complex rules for transaction monitoring
✅ **Multi-Account Management** - Support multiple accounts per business customer
✅ **Counterparty Risk Scoring** - Track and manage counterparty relationships
✅ **Approval Handling** - Review and approve unusual transactions
✅ **Audit Trail** - Complete change history for compliance
✅ **Real-Time Monitoring** - Monitor vs Enforce modes
✅ **OpenAPI Documentation** - Interactive API explorer

## 🚀 Deployment

### Azure Deployment
- GitHub Actions pipeline in `.github/workflows/ci.yml`
- Builds and deploys on push to main
- PostgreSQL managed database (Supabase)

### Environment Variables
```
ASPNETCORE_ENVIRONMENT=Production
DefaultConnection=<Supabase PostgreSQL connection string>
CORS_ORIGIN=<Frontend URL>
```

## 📝 Database Schema

**Core Entities**:
- Banks
- BusinessCustomers
- Accounts
- Policies & PolicyRules
- Counterparties
- Approvals
- PaymentEvents
- Decisions
- AuditLog

## 🔐 Security Considerations

- GitHub OAuth integration for authentication
- Role-based access control (ready for implementation)
- Encrypted sensitive data storage
- Audit trail for all operations
- SQL injection protection via EF Core parameterization

## 📄 License

Proprietary - Confidential

---

**Repository**: https://github.com/garycto/remitmon  
**Documentation**: http://localhost:5199/api-docs.html
