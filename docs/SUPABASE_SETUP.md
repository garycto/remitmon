# RemitMon - Supabase & GitHub OAuth Setup

This guide shows how to set up RemitMon with Supabase (PostgreSQL) and GitHub OAuth authentication.

## 🔧 Prerequisites

- Supabase account (free at https://supabase.com)
- GitHub OAuth App (for authentication)
- .NET 8 SDK
- Node.js 20+

---

## 1️⃣ **Create Supabase Project**

### Step 1: Create Supabase Project
1. Go to https://supabase.com and sign in
2. Click **"New Project"**
3. Enter project details:
   - **Name**: `remitmon`
   - **Database Password**: Create a strong password (save it!)
   - **Region**: Choose closest to you
   - **Pricing**: Free tier is fine for development

### Step 2: Get Connection String
Once your project is created:
1. Go to **Settings** → **Database**
2. Under "Connection Pooling", copy the **Connection string**
3. It will look like:
   ```
   postgresql://postgres:[PASSWORD]@db.[PROJECT_ID].supabase.co:6543/postgres?sslmode=require
   ```
4. Replace `[PASSWORD]` with your database password

### Step 3: Get API Keys
Go to **Settings** → **API**:
- Copy **Project URL** (e.g., `https://[PROJECT_ID].supabase.co`)
- Copy **Anon Public key**
- Copy **Service Role Secret key**

---

## 2️⃣ **Setup GitHub OAuth**

### Step 1: Create GitHub OAuth App
1. Go to https://github.com/settings/developers
2. Click **"OAuth Apps"** → **"New OAuth App"**
3. Fill in:
   - **Application name**: `RemitMon`
   - **Homepage URL**: `http://localhost:5173`
   - **Authorization callback URL**: `http://localhost:5000/auth/github/callback`
4. Click **"Register application"**
5. You'll see:
   - **Client ID** (copy this)
   - **Client Secret** (click to reveal, copy this)

### Step 2: Enable GitHub Auth in Supabase (Optional)
In Supabase dashboard:
1. Go to **Authentication** → **Providers**
2. Click **GitHub** and enable it
3. Paste your GitHub Client ID and Secret
4. Save

---

## 3️⃣ **Configure RemitMon**

### Step 1: Update appsettings.json
Edit `src/RemitMon.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "postgresql://postgres:[PASSWORD]@db.[PROJECT_ID].supabase.co:6543/postgres?sslmode=require"
  },
  "Authentication": {
    "GitHub": {
      "ClientId": "YOUR_GITHUB_CLIENT_ID",
      "ClientSecret": "YOUR_GITHUB_CLIENT_SECRET"
    },
    "Supabase": {
      "Url": "https://[PROJECT_ID].supabase.co",
      "AnonKey": "YOUR_ANON_KEY",
      "ServiceRoleKey": "YOUR_SERVICE_ROLE_KEY"
    }
  },
  "Auth": {
    "Mode": "Production",
    "Provider": "GitHub"
  },
  "CORS_ORIGIN": "http://localhost:5173"
}
```

Replace:
- `[PASSWORD]` → Your Supabase database password
- `[PROJECT_ID]` → Your Supabase project ID
- `YOUR_GITHUB_CLIENT_ID` → From GitHub OAuth App
- `YOUR_GITHUB_CLIENT_SECRET` → From GitHub OAuth App
- `YOUR_ANON_KEY` → From Supabase Settings
- `YOUR_SERVICE_ROLE_KEY` → From Supabase Settings

### Step 2: Update Frontend Configuration
Edit `web/src/lib/api.ts`:

```typescript
const API_URL = process.env.VITE_API_URL || 'http://localhost:5000';

// Add GitHub auth redirect
export const githubLoginUrl = `${API_URL}/auth/login?returnUrl=${window.location.origin}`;

export const logout = async () => {
  await apiClient.get('/auth/logout');
  window.location.href = '/';
};
```

### Step 3: Update Frontend Vite Config
Edit `web/vite.config.ts`:

```typescript
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/auth': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
});
```

---

## 🚀 **Running RemitMon**

### Terminal 1: Backend API
```bash
cd "/Users/garylazarus/Source/Remit Monitor"
dotnet restore
dotnet run --project src/RemitMon.Api
```

**Wait for**:
```
[10:00:13 INF] Now listening on: http://localhost:5000
```

✅ API ready at http://localhost:5000/swagger

### Terminal 2: Frontend
```bash
cd "/Users/garylazarus/Source/Remit Monitor/web"
npm install
npm run dev
```

**Wait for**:
```
Local: http://localhost:5173
```

✅ Frontend ready at http://localhost:5173

---

## ✅ **Testing Authentication**

1. Open http://localhost:5173
2. Click **"Login with GitHub"**
3. You'll be redirected to GitHub to authorize
4. After authorization, you're logged in as that GitHub user

**Check current user**:
```bash
curl http://localhost:5000/auth/me \
  -H "Cookie: cookies=YOUR_SESSION_COOKIE"
```

---

## 🔐 **Security Notes**

✅ Never commit `ClientSecret` or connection strings
✅ Use `.gitignore` to exclude `appsettings.Production.json`
✅ Store secrets in Azure Key Vault for production
✅ Use environment variables for sensitive config:

```bash
export "ConnectionStrings__DefaultConnection=postgresql://..."
export "Authentication__GitHub__ClientId=..."
export "Authentication__GitHub__ClientSecret=..."

dotnet run --project src/RemitMon.Api
```

---

## 📊 **Supabase Features Available**

Your RemitMon project has access to:
- ✅ **PostgreSQL** database (RemitMon schema automatically created)
- ✅ **Real-time** subscriptions (for future SignalR integration)
- ✅ **Storage** buckets (for files, documents)
- ✅ **Functions** (serverless backend)
- ✅ **Webhooks** (for external integrations)
- ✅ **Vector search** (Pgvector extension)

---

## 🆘 **Troubleshooting**

### Connection String Errors
```
"database "remitmon" does not exist"
```
→ The database is named `postgres` by default in Supabase. No need to create RemitMon.

### GitHub OAuth Redirect Error
```
"redirect_uri_mismatch"
```
→ Make sure your GitHub OAuth App has:
- Authorization callback URL: `http://localhost:5000/auth/github/callback`

### Supabase Connection Timeout
```
"unable to connect to database"
```
→ Check:
1. Project is active in Supabase dashboard
2. Connection string is correct (no typos)
3. Database password is correct
4. You're using `sslmode=require`

---

## 🎯 **Next Steps**

1. ✅ Create Supabase project
2. ✅ Create GitHub OAuth app
3. ✅ Configure appsettings.json
4. ✅ Run backend: `dotnet run --project src/RemitMon.Api`
5. ✅ Run frontend: `npm run dev` (in /web)
6. ✅ Test login with GitHub
7. 🚀 Deploy to Azure when ready

---

## 📚 **Documentation**

- Supabase Docs: https://supabase.com/docs
- GitHub OAuth: https://docs.github.com/en/developers/apps/building-oauth-apps
- RemitMon Architecture: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)

---

**Ready to run!** No Docker needed. Just Supabase + GitHub auth. 🚀
