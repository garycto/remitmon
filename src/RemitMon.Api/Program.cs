using Serilog;
using RemitMon.Infrastructure;
using RemitMon.Infrastructure.Seeding;
using RemitMon.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Azure.Identity;
using RemitMon.Infrastructure.Adapters;

// Load a repository-local environment file only for Development.
// This is configuration loading, not secret logging.
var aspnetEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.Equals(aspnetEnvironment, "Development", StringComparison.OrdinalIgnoreCase)
    || string.IsNullOrWhiteSpace(aspnetEnvironment))
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env.local"),
        Path.Combine(Directory.GetCurrentDirectory(), "local.env"),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env.local")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "local.env")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env.local")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "local.env"))
    };
    var localEnvironmentFile = candidates.FirstOrDefault(File.Exists);
    if (localEnvironmentFile is not null)
    {
        foreach (var line in File.ReadAllLines(localEnvironmentFile))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;
            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
                continue;
            var key = trimmed[..separator].Trim();
            var value = trimmed[(separator + 1)..].Trim();
            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

var keyVaultUrl = builder.Configuration["KeyVault:Url"];
if (!string.IsNullOrWhiteSpace(keyVaultUrl)
    && Uri.TryCreate(keyVaultUrl, UriKind.Absolute, out var keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}

// Configure Serilog
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .MinimumLevel.Information()
        .WriteTo.Console()
        .WriteTo.File("logs/remitmon-.txt", rollingInterval: RollingInterval.Day);
});

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure CORS
var corsOrigin = builder.Configuration["CORS_ORIGIN"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                corsOrigin,
                "http://localhost:5173",
                "http://127.0.0.1:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Authentication (GitHub OAuth via Supabase)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/denied";
    });

// Add Authorization
builder.Services.AddAuthorization();

builder.Services
    .AddOptions<ModernTreasuryOptions>()
    .Configure(options =>
    {
        builder.Configuration.GetSection(ModernTreasuryOptions.SectionName).Bind(options);
        options.IntegrationsMode = builder.Configuration["Integrations:Mode"];
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<JackHenryOptions>()
    .Configure(options =>
    {
        builder.Configuration.GetSection(JackHenryOptions.SectionName).Bind(options);
        options.IntegrationsMode = builder.Configuration["JackHenry:Mode"] ?? "Simulator";
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddTransient<ModernTreasuryRetryHandler>();
builder.Services
    .AddHttpClient<RealModernTreasuryAdapter>()
    .AddHttpMessageHandler<ModernTreasuryRetryHandler>();
builder.Services
    .AddHttpClient<RealJackHenryAdapter>()
    .AddHttpMessageHandler<ModernTreasuryRetryHandler>();
builder.Services.AddScoped<SimulatorPaymentOrchestrationAdapter>();
builder.Services.AddScoped<IPaymentOrchestrationAdapter>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ModernTreasuryOptions>>().Value;
    return options.IsLive
        ? serviceProvider.GetRequiredService<RealModernTreasuryAdapter>()
        : serviceProvider.GetRequiredService<SimulatorPaymentOrchestrationAdapter>();
});

// Add DbContext with Supabase connection
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connString))
{
    throw new InvalidOperationException(
        "DefaultConnection is not configured. Please set Supabase connection string in appsettings.json");
}

var npgsqlBuilder = new Npgsql.NpgsqlDataSourceBuilder(connString);
npgsqlBuilder.EnableDynamicJson();
var dataSource = npgsqlBuilder.Build();

builder.Services.AddDbContext<RemitMonDbContext>(options =>
{
    options.UseNpgsql(dataSource);
});

var app = builder.Build();

// Initialize the database and seed demo data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<RemitMonDbContext>();
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw(ApprovalSchemaMigration.RenameLegacyTableSql);

        // Seed demo data in development
        if (app.Environment.IsDevelopment())
        {
            DemoDataSeeder.SeedDemoData(db);
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database initialization failed");
        logger.LogWarning("The API will continue with database-backed endpoints unavailable.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health");

app.MapGet("/auth/me", (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated ?? false)
    {
        return Results.Ok(new
        {
            id = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            login = context.User.FindFirst(ClaimTypes.Name)?.Value,
            avatar = context.User.FindFirst("urn:github:avatar_url")?.Value
        });
    }
    return Results.Unauthorized();
})
    .WithName("CurrentUser");

app.MapGet("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
})
    .WithName("Logout");

app.MapAccountEndpoints();
app.MapPolicyEndpoints();
app.MapCounterpartyEndpoints();
app.MapApprovalEndpoints();
app.MapDashboardEndpoints();
app.MapModernTreasuryEndpoints();   // the only new line
app.MapJackHenryEndpoints();

app.Run();
