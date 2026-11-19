# RemitMon — Account Protection Platform

> A bank-distributed account protection platform that places configurable, deterministic financial guardrails around business bank accounts.

## 🚀 Quick Start

**F5 to run everything** (VS Code):
- Builds backend (.NET 10)
- Starts API on http://localhost:5000
- Starts frontend on http://localhost:5173
- Opens browser automatically

Or manually:

```bash
# Terminal 1: Backend API
dotnet run --project src/RemitMon.Api
# → http://localhost:5000/swagger

# Terminal 2: Frontend
cd web && npm install && npm run dev
# → http://localhost:5173
```

**Before first run:** Configure Supabase + GitHub OAuth in `src/RemitMon.Api/appsettings.json` (see [SUPABASE_SETUP.md](SUPABASE_SETUP.md))

---

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| [QUICKSTART.md](QUICKSTART.md) | 2-minute setup guide |
| [SUPABASE_SETUP.md](SUPABASE_SETUP.md) | Supabase + GitHub OAuth configuration |
| [README.md](README.md) | Full feature & architecture overview |
| [ARCHITECTURE.md](ARCHITECTURE.md) | System design & technical details |
| [DECISIONS.md](DECISIONS.md) | Why we chose these technologies |
| [REMITMON.md](REMITMON.md) | Original specification |

---

## 🏗️ Project Structure

```
remitmon/
├── src/
│   ├── RemitMon.Api              # ASP.NET Core REST API
│   ├── RemitMon.Domain           # 15 entities + 13 enums
│   ├── RemitMon.Engine           # Policy evaluation engine
│   ├── RemitMon.Engine.Tests     # Unit tests (9 passing)
│   └── RemitMon.Infrastructure   # EF Core + Supabase
├── web/                          # React 18 + TypeScript + Vite
├── docs/                         # All documentation
├── infra/                        # Bicep IaC for Azure
├── .vscode/                      # Launch & debug config (F5)
└── RemitMon.sln                  # Solution file
```

---

## ✨ Features

- ✅ **Supabase PostgreSQL** database (cloud)
- ✅ **GitHub OAuth** authentication
- ✅ **10-rule policy** evaluation engine
- ✅ **React dashboard** with charts
- ✅ **REST API** with Swagger docs
- ✅ **EF Core 10** with migrations
- ✅ **Full-stack debugging** (F5)
- ✅ **Azure deployment** ready (Bicep IaC)
- ✅ **GitHub Actions** CI/CD

---

## 🛠️ Tech Stack

**Backend**:
- .NET 10 / ASP.NET Core
- EF Core 10 with PostgreSQL
- Serilog logging
- OpenAPI/Swagger

**Frontend**:
- React 18
- TypeScript
- Vite
- Tailwind CSS
- TanStack Query

**Infrastructure**:
- Supabase (PostgreSQL)
- GitHub OAuth
- Azure (Container Apps, Static Web Apps)
- Bicep IaC

---

## 🧪 Build & Test

```bash
# Build
dotnet build

# Test
dotnet test

# Frontend build
cd web && npm run build
```

**Status**: ✅ Build passing | ✅ 9/9 tests passing | ✅ Zero warnings/errors

---

## 📖 Learn More

- **Setting up Supabase + GitHub OAuth**: [SUPABASE_SETUP.md](SUPABASE_SETUP.md)
- **Full architecture overview**: [README.md](README.md)
- **System design & data flows**: [ARCHITECTURE.md](ARCHITECTURE.md)
- **Original specification**: [REMITMON.md](REMITMON.md)

---

**Made with ❤️ using Copilot**
