using Microsoft.EntityFrameworkCore;
using RemitMon.Api.Dtos;
using RemitMon.Domain.Entities;
using RemitMon.Infrastructure;

namespace RemitMon.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounts")
            .WithName("Accounts");

        group.MapGet("/", GetAccounts)
            .WithName("GetAccounts")
            .WithSummary("Get all accounts");

        group.MapGet("/{id}", GetAccount)
            .WithName("GetAccount")
            .WithSummary("Get account by ID");

        group.MapPost("/", CreateAccount)
            .WithName("CreateAccount")
            .WithSummary("Create new account");

        group.MapPatch("/{id}/mode", UpdateAccountMode)
            .WithName("UpdateAccountMode")
            .WithSummary("Update account mode (Monitor/Enforce)");
    }

    private static async Task<IResult> GetAccounts(RemitMonDbContext db)
    {
        var accounts = await db.Accounts
            .Select(a => new AccountDto(
                a.Id,
                a.BusinessCustomerId,
                a.BankId,
                a.DisplayName,
                a.MaskedAccountNumber,
                a.Status,
                a.Mode,
                a.CreatedAt,
                a.UpdatedAt
            ))
            .ToListAsync();

        return Results.Ok(accounts);
    }

    private static async Task<IResult> GetAccount(Guid id, RemitMonDbContext db)
    {
        var account = await db.Accounts
            .Where(a => a.Id == id)
            .Select(a => new AccountDto(
                a.Id,
                a.BusinessCustomerId,
                a.BankId,
                a.DisplayName,
                a.MaskedAccountNumber,
                a.Status,
                a.Mode,
                a.CreatedAt,
                a.UpdatedAt
            ))
            .FirstOrDefaultAsync();

        if (account is null)
            return Results.NotFound();

        return Results.Ok(account);
    }

    private static async Task<IResult> CreateAccount(CreateAccountRequest request, RemitMonDbContext db)
    {
        var account = new Account
        {
            BusinessCustomerId = request.BusinessCustomerId,
            BankId = request.BankId,
            DisplayName = request.DisplayName,
            MaskedAccountNumber = request.MaskedAccountNumber
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/accounts/{account.Id}", new AccountDto(
            account.Id,
            account.BusinessCustomerId,
            account.BankId,
            account.DisplayName,
            account.MaskedAccountNumber,
            account.Status,
            account.Mode,
            account.CreatedAt,
            account.UpdatedAt
        ));
    }

    private static async Task<IResult> UpdateAccountMode(Guid id, UpdateAccountModeRequest request, RemitMonDbContext db)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (account is null)
            return Results.NotFound();

        account.Mode = request.Mode;
        account.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new AccountDto(
            account.Id,
            account.BusinessCustomerId,
            account.BankId,
            account.DisplayName,
            account.MaskedAccountNumber,
            account.Status,
            account.Mode,
            account.CreatedAt,
            account.UpdatedAt
        ));
    }
}
