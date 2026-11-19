# RemitMon Quick Start - Supabase + GitHub Auth

## 🚀 Setup (One-Time)

### 1. Create Supabase Project
- Go to https://supabase.com and create a free project
- Save the **connection string** (you'll need this)
- Note your **Project URL** and **API keys**

### 2. Create GitHub OAuth App
- Go to https://github.com/settings/developers
- Create new **OAuth App**
- **Authorization callback URL**: `http://localhost:5000/auth/github/callback`
- Save your **Client ID** and **Client Secret**

### 3. Configure RemitMon
Edit `src/RemitMon.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_SUPABASE_CONNECTION_STRING"
  },
  "Authentication": {
    "GitHub": {
      "ClientId": "YOUR_GITHUB_CLIENT_ID",
      "ClientSecret": "YOUR_GITHUB_CLIENT_SECRET"
    },
    "Supabase": {
      "Url": "https://YOUR_PROJECT.supabase.co",
      "AnonKey": "YOUR_ANON_KEY",
      "ServiceRoleKey": "YOUR_SERVICE_ROLE_KEY"
    }
  }
}
```

---

## ▶️ Running RemitMon

### Terminal 1: Start Backend
```bash
cd "/Users/garylazarus/Source/Remit Monitor"
dotnet run --project src/RemitMon.Api
```

**Wait for**:
```
Now listening on: http://localhost:5000
```

✅ API ready at: **http://localhost:5000/swagger**

### Terminal 2: Start Frontend
```bash
cd "/Users/garylazarus/Source/Remit Monitor/web"
npm install  # (first time only)
npm run dev
```

**Wait for**:
```
Local: http://localhost:5173
```

✅ Frontend ready at: **http://localhost:5173**

---

## ✅ Login with GitHub

1. Open **http://localhost:5173**
2. Click **"Login with GitHub"**
3. Authorize the app
4. ✅ You're logged in!

---

## 🛑 To Stop

```bash
Ctrl+C  # (in both terminals)
```

---

## 📚 More Info

- **Full setup guide**: [SUPABASE_SETUP.md](SUPABASE_SETUP.md)
- **Architecture docs**: [ARCHITECTURE.md](ARCHITECTURE.md)
- **API Docs**: http://localhost:5000/swagger (when running)

---

**That's it! No Docker, just Supabase + GitHub auth.** 🎉
