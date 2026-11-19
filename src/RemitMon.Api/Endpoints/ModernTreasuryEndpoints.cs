using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RemitMon.Domain.Entities;
using RemitMon.Domain.Enums;
using RemitMon.Infrastructure;
using RemitMon.Infrastructure.Adapters;

namespace RemitMon.Api.Endpoints;

public static class ModernTreasuryEndpoints
{
    public static IEndpointRouteBuilder MapModernTreasuryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/modern-treasury")
            .WithTags("Modern Treasury");

        group.MapGet("/counterparties", async (
            IPaymentOrchestrationAdapter adapter,
            Guid? accountId,
            RemitMonDbContext db,
            CancellationToken cancellationToken) =>
        {
            Guid businessCustomerId;
            if (accountId.HasValue)
            {
                businessCustomerId = await db.Accounts
                    .AsNoTracking()
                    .Where(account => account.Id == accountId.Value)
                    .Select(account => account.BusinessCustomerId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (businessCustomerId == Guid.Empty)
                    return Results.NotFound();
            }
            else
            {
                businessCustomerId = await db.BusinessCustomers
                    .AsNoTracking()
                    .Select(customer => customer.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var result = await adapter.ListCounterpartiesAsync(businessCustomerId, cancellationToken);
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

        group.MapGet("/payment-orders", async (
            IPaymentOrchestrationAdapter adapter,
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

        group.MapGet("/incoming-payment-details", async (
            IPaymentOrchestrationAdapter adapter,
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

        group.MapGet("/payment-orders/{paymentOrderId}", async (
            string paymentOrderId,
            IPaymentOrchestrationAdapter adapter,
            Guid? accountId,
            RemitMonDbContext db,
            CancellationToken cancellationToken) =>
        {
            var account = await ResolveAccountAsync(accountId, db, cancellationToken);
            if (account is null)
                return Results.NotFound();
            var result = await adapter.GetPaymentOrderAsync(account.Id, paymentOrderId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(MapPaymentDto(result));
        });

        app.MapPost("/webhooks/bank/moderntreasury", async (
            HttpRequest request,
            IOptions<ModernTreasuryOptions> options,
            RemitMonDbContext db,
            CancellationToken cancellationToken) =>
        {
            var signingKey = options.Value.WebhookSigningKey;
            if (string.IsNullOrWhiteSpace(signingKey))
                return Results.Problem("Modern Treasury webhook signing key is not configured.", statusCode: 503);

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var payload = await reader.ReadToEndAsync(cancellationToken);
            var signature = request.Headers["X-Signature"].ToString();
            if (string.IsNullOrWhiteSpace(signature))
                return Results.Unauthorized();

            var verifier = new ModernTreasuryWebhookVerifier();
            if (!verifier.Verify(payload, signature, signingKey))
                return Results.Unauthorized();

            var eventId = request.Headers["X-Webhook-ID"].ToString();
            if (string.IsNullOrWhiteSpace(eventId))
                eventId = request.Headers["X-Event-ID"].ToString();
            if (string.IsNullOrWhiteSpace(eventId))
                return Results.BadRequest("Missing X-Webhook-ID or X-Event-ID.");

            if (await db.PaymentEvents.AsNoTracking().AnyAsync(
                payment => payment.ExternalEventId == eventId,
                cancellationToken))
                return Results.Ok(new { received = true, duplicate = true });

            var account = await db.Accounts
                .AsNoTracking()
                .OrderBy(account => account.CreatedAt)
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

            db.PaymentEvents.Add(new PaymentEvent
            {
                AccountId = account.Id,
                ExternalEventId = eventId,
                Rail = Rail.ACH,
                Direction = PaymentDirection.Inbound,
                Initiation = PaymentInitiation.Pull,
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

    private static async Task<Account?> ResolveAccountAsync(
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

    private static object MapPaymentDto(PaymentEvent payment) => new
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
