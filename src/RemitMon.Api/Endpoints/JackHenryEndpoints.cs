using Microsoft.EntityFrameworkCore;
using RemitMon.Infrastructure;
using RemitMon.Infrastructure.Adapters;

namespace RemitMon.Api.Endpoints;

public static class JackHenryEndpoints
{
    public static IEndpointRouteBuilder MapJackHenryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/jack-henry")
            .WithTags("Jack Henry");

        group.MapGet("/accounts", async (
            Microsoft.Extensions.Options.IOptions<JackHenryOptions> options,
            RealJackHenryAdapter adapter,
            CancellationToken cancellationToken) =>
        {
            if (!options.Value.IsLive)
            {
                return Results.Ok(new[]
                {
                    new
                    {
                        Id = "jh-sandbox-usd",
                        Name = "USD Deposit",
                        MaskedAccountNumber = "****9999",
                        AccountType = "Deposit",
                        Currency = "USD",
                        Balance = 1000.00m,
                        Status = "Sandbox"
                    },
                    new
                    {
                        Id = "jh-sandbox-rtp",
                        Name = "RTP Virtual Account",
                        MaskedAccountNumber = "****4321",
                        AccountType = "Virtual",
                        Currency = "USD",
                        Balance = 250.00m,
                        Status = "Sandbox"
                    },
                    new
                    {
                        Id = "jh-sandbox-wire",
                        Name = "Wire Test Account",
                        MaskedAccountNumber = "****7788",
                        AccountType = "Deposit",
                        Currency = "USD",
                        Balance = 5000.00m,
                        Status = "Sandbox"
                    }
                });
            }
            var result = await adapter.ListAccountsAsync(cancellationToken);
            return Results.Ok(result);
        });

        group.MapGet("/counterparties", async (
            RealJackHenryAdapter adapter,
            Guid? accountId,
            RemitMonDbContext db,
            CancellationToken cancellationToken) =>
        {
            var account = await ResolveAccountAsync(accountId, db, cancellationToken);
            if (account is null)
                return Results.NotFound();
            var result = await adapter.ListCounterpartiesAsync(account.BusinessCustomerId, cancellationToken);
            return Results.Ok(result.Select(counterparty => new
            {
                counterparty.Id,
                counterparty.Name,
                Status = counterparty.Status.ToString(),
                Source = counterparty.Source.ToString(),
                counterparty.RoutingAccountFingerprints,
                counterparty.CreatedAt,
                counterparty.UpdatedAt
            }));
        });

        group.MapGet("/transactions", async (
            RealJackHenryAdapter adapter,
            Guid? accountId,
            RemitMonDbContext db,
            CancellationToken cancellationToken) =>
        {
            var account = await ResolveAccountAsync(accountId, db, cancellationToken);
            if (account is null)
                return Results.NotFound();
            var result = await adapter.ListIncomingPaymentDetailsAsync(account.Id, null, cancellationToken);
            return Results.Ok(result.Select(MapPaymentDto));
        });

        group.MapGet("/payments", async (
            RealJackHenryAdapter adapter,
            Guid? accountId,
            RemitMonDbContext db,
            CancellationToken cancellationToken) =>
        {
            var account = await ResolveAccountAsync(accountId, db, cancellationToken);
            if (account is null)
                return Results.NotFound();
            var result = await adapter.ListPaymentOrdersAsync(account.Id, null, cancellationToken);
            return Results.Ok(result.Select(MapPaymentDto));
        });

        group.MapGet("/payments/{paymentId}", async (
            string paymentId,
            RealJackHenryAdapter adapter,
            Guid? accountId,
            RemitMonDbContext db,
            CancellationToken cancellationToken) =>
        {
            var account = await ResolveAccountAsync(accountId, db, cancellationToken);
            if (account is null)
                return Results.NotFound();
            var result = await adapter.GetPaymentOrderAsync(account.Id, paymentId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(MapPaymentDto(result));
        });

        app.MapPost("/webhooks/bank/jackhenry", async (
            HttpRequest request,
            Microsoft.Extensions.Options.IOptions<JackHenryOptions> options,
            RemitMonDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(options.Value.WebhookSigningKey))
                return Results.Problem("Jack Henry webhook signing key is not configured.", statusCode: 503);

            using var reader = new StreamReader(request.Body);
            var payload = await reader.ReadToEndAsync(cancellationToken);
            var signature = request.Headers["X-Signature"].ToString();
            var verifier = new ModernTreasuryWebhookVerifier();
            if (!verifier.Verify(payload, signature, options.Value.WebhookSigningKey))
                return Results.Unauthorized();

            var eventId = request.Headers["X-Event-ID"].ToString();
            if (string.IsNullOrWhiteSpace(eventId))
                eventId = request.Headers["X-Webhook-ID"].ToString();
            if (string.IsNullOrWhiteSpace(eventId))
                return Results.BadRequest("Missing Jack Henry event ID.");

            if (await db.PaymentEvents.AsNoTracking().AnyAsync(
                payment => payment.ExternalEventId == eventId,
                cancellationToken))
                return Results.Ok(new { received = true, duplicate = true });

            var account = await db.Accounts.AsNoTracking().OrderBy(account => account.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (account is null)
                return Results.Problem("No demo account is available for webhook ingestion.", statusCode: 503);

            using var document = System.Text.Json.JsonDocument.Parse(payload);
            var data = document.RootElement.TryGetProperty("data", out var dataElement)
                ? dataElement
                : document.RootElement;
            var rawPayload = data.EnumerateObject().ToDictionary(
                property => property.Name,
                property => (object)JsonElementToObject(property.Value));

            db.PaymentEvents.Add(new RemitMon.Domain.Entities.PaymentEvent
            {
                AccountId = account.Id,
                ExternalEventId = eventId,
                Rail = RemitMon.Domain.Enums.Rail.ACH,
                Direction = RemitMon.Domain.Enums.PaymentDirection.Inbound,
                Initiation = RemitMon.Domain.Enums.PaymentInitiation.Push,
                Amount = 0m,
                Currency = "USD",
                Reference = eventId,
                EventTime = DateTime.UtcNow,
                RawPayload = rawPayload,
                CorrelationId = eventId
            });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { received = true, duplicate = false });
        })
        .AllowAnonymous();

        return app;
    }

    private static async Task<RemitMon.Domain.Entities.Account?> ResolveAccountAsync(
        Guid? accountId,
        RemitMonDbContext db,
        CancellationToken cancellationToken)
    {
        if (accountId.HasValue)
            return await db.Accounts.AsNoTracking().FirstOrDefaultAsync(
                account => account.Id == accountId.Value,
                cancellationToken);
        return await db.Accounts.AsNoTracking().OrderBy(account => account.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static object MapPaymentDto(RemitMon.Domain.Entities.PaymentEvent payment) => new
    {
        payment.Id,
        payment.ExternalEventId,
        payment.AccountId,
        Rail = payment.Rail.ToString(),
        Direction = payment.Direction.ToString(),
        Initiation = payment.Initiation.ToString(),
        payment.Amount,
        payment.Currency,
        payment.CounterpartyRef,
        payment.Reference,
        payment.Memo,
        payment.InvoiceRef,
        payment.EventTime,
        payment.SettlementDate
    };

    private static object JsonElementToObject(System.Text.Json.JsonElement element) => element.ValueKind switch
    {
        System.Text.Json.JsonValueKind.String => element.GetString() ?? string.Empty,
        System.Text.Json.JsonValueKind.Number => element.TryGetDecimal(out var decimalValue) ? decimalValue : element.GetRawText(),
        System.Text.Json.JsonValueKind.True => true,
        System.Text.Json.JsonValueKind.False => false,
        System.Text.Json.JsonValueKind.Null => null!,
        System.Text.Json.JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
        System.Text.Json.JsonValueKind.Object => element.EnumerateObject().ToDictionary(
            property => property.Name,
            property => JsonElementToObject(property.Value)),
        _ => element.GetRawText()
    };
}
