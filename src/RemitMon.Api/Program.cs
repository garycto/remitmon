using Serilog;
using RemitMon.Infrastructure;
using RemitMon.Infrastructure.Seeding;
using RemitMon.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

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
        policy.WithOrigins(corsOrigin)
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

// Add DbContext with Supabase connection
builder.Services.AddDbContext<RemitMonDbContext>(options =>
{
    var connString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connString))
    {
        throw new InvalidOperationException(
            "DefaultConnection is not configured. Please set Supabase connection string in appsettings.json");
    }
    options.UseNpgsql(connString);
});

var app = builder.Build();

// Apply migrations and seed demo data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<RemitMonDbContext>();
        db.Database.EnsureCreated();
        
        // Seed demo data in development
        if (app.Environment.IsDevelopment())
        {
            DemoDataSeeder.SeedDemoData(db);
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Could not initialize database: {Message}", ex.Message);
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
app.MapExceptionEndpoints();

app.Run();

