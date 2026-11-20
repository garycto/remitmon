using Microsoft.EntityFrameworkCore;
using RemitMon.Api.Dtos;
using RemitMon.Infrastructure;
using RemitMon.Domain.Entities;
using RemitMon.Domain.Enums;
using System.Text.Json;

namespace RemitMon.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/dashboard/activity", GetActivity)
            .WithName("GetDashboardActivity")
            .WithSummary("Get expected payments and recent payment activity");
    }

    private static async Task<IResult> GetActivity(Guid? accountId, RemitMonDbContext db)
    {
        Guid? businessCustomerId = null;
        if (accountId.HasValue)
        {
            businessCustomerId = await db.Accounts
                .AsNoTracking()
                .Where(account => account.Id == accountId.Value)
                .Select(account => (Guid?)account.BusinessCustomerId)
                .FirstOrDefaultAsync();

            if (!businessCustomerId.HasValue)
                return Results.NotFound();
        }

        var obligationsQuery = db.ExpectedObligations
            .AsNoTracking()
            .AsQueryable();
        if (businessCustomerId.HasValue)
            obligationsQuery = obligationsQuery.Where(obligation => obligation.BusinessCustomerId == businessCustomerId.Value);

        var expectedPayments = await obligationsQuery
            .OrderBy(obligation => obligation.DueDate)
            .Take(100)
            .ToListAsync();

        var paymentQuery = db.PaymentEvents
            .AsNoTracking()
            .Include(payment => payment.Account)
            .AsQueryable();
        if (accountId.HasValue)
            paymentQuery = paymentQuery.Where(payment => payment.AccountId == accountId.Value);

        var payments = await paymentQuery
            .OrderByDescending(payment => payment.EventTime)
            .ToListAsync();

        var accountIds = payments.Select(payment => payment.AccountId).Distinct().ToList();
        var policies = await db.Policies
            .AsNoTracking()
            .Include(policy => policy.Rules)
            .Where(policy => accountIds.Contains(policy.AccountId) && policy.Status == PolicyStatus.Active)
            .ToListAsync();
        var expectedDtos = expectedPayments.Select(obligation => new DashboardExpectedPaymentDto(
            obligation.Id,
            obligation.Source.ToString(),
            obligation.SourceRef,
            obligation.CounterpartyName,
            obligation.Amount,
            obligation.DueDate,
            obligation.Status.ToString(),
            obligation.MatchedPaymentEventId
        )).ToList();

        var paymentDtos = payments.Select(payment =>
        {
            var obligation = FindMatchedObligation(payment, expectedDtos);
            var policy = policies
                .Where(candidate => candidate.AccountId == payment.AccountId)
                .OrderByDescending(candidate => candidate.Version)
                .FirstOrDefault();
            var evaluation = EvaluatePayment(payment, policy, payments, obligation);
            var achInstantEligible = IsAchInstantEligible(payment);
            var convertedToInstant = IsConvertedToInstant(payment);
            return new DashboardPaymentDto(
                payment.Id,
                payment.AccountId,
                payment.Account!.DisplayName,
                payment.Rail.ToString(),
                payment.Direction.ToString(),
                payment.Initiation.ToString(),
                payment.Amount,
                payment.Currency,
                payment.CounterpartyRef,
                payment.Memo,
                payment.Reference,
                payment.InvoiceRef,
                payment.EventTime,
                payment.SettlementDate,
                obligation is null ? "Unmatched" : "Matched",
                obligation?.Id,
                obligation?.SourceRef,
                evaluation.Outcome,
                policy?.Id,
                policy?.Name ?? "No active policy",
                evaluation.RuleHits,
                achInstantEligible,
                convertedToInstant
            );
        }).ToList();

        var eligible = paymentDtos.Count(payment => payment.AchInstantEligible);
        var converted = paymentDtos.Count(payment => payment.AchInstantEligible && payment.ConvertedToInstant);
        var achCount = paymentDtos.Count(payment => payment.Rail == Rail.ACH.ToString());

        return Results.Ok(new DashboardActivityDto(
            expectedDtos,
            paymentDtos,
            new AchInstantOpportunityDto(eligible, converted, achCount - eligible)
        ));
    }

    private static DashboardExpectedPaymentDto? FindMatchedObligation(
        PaymentEvent payment,
        IReadOnlyList<DashboardExpectedPaymentDto> obligations)
    {
        var exactInvoice = obligations.FirstOrDefault(obligation =>
            (!string.IsNullOrWhiteSpace(payment.InvoiceRef)
             && string.Equals(obligation.SourceRef, payment.InvoiceRef, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(payment.Reference)
             && string.Equals(obligation.SourceRef, payment.Reference, StringComparison.OrdinalIgnoreCase)));
        if (exactInvoice is not null)
            return exactInvoice;

        return obligations.FirstOrDefault(obligation =>
            !string.IsNullOrWhiteSpace(payment.CounterpartyRef)
            && string.Equals(obligation.CounterpartyName, payment.CounterpartyRef, StringComparison.OrdinalIgnoreCase)
            && Math.Abs(obligation.Amount - payment.Amount) <= 0.01m);
    }

    private static PaymentEvaluation EvaluatePayment(
        PaymentEvent payment,
        Policy? policy,
        IReadOnlyList<PaymentEvent> recentPayments,
        DashboardExpectedPaymentDto? obligation)
    {
        if (payment.Direction == PaymentDirection.Outbound)
            return new PaymentEvaluation(
                obligation is null ? "Allowed" : "Allowed",
                obligation is null
                    ? ["Outbound activity - no inbound rule applied"]
                    : ["Invoice or obligation matched"]);

        if (policy is null)
            return new PaymentEvaluation("Review", ["No active policy"]);

        var hits = new List<string>();
        var outcomes = new List<DecisionOutcome>();

        foreach (var rule in policy.Rules.Where(rule => rule.Enabled).OrderBy(rule => rule.Priority))
        {
            var ruleType = rule.RuleType.ToLowerInvariant();
            if (ruleType.Contains("highvalue") || ruleType.Contains("maxamount"))
            {
                var threshold = GetDecimal(rule.Parameters, "threshold")
                    ?? GetDecimal(rule.Parameters, "amount");
                if (threshold.HasValue && payment.Amount >= threshold.Value)
                {
                    outcomes.Add(rule.Action);
                    hits.Add($"{rule.RuleType}: amount {payment.Amount:C} meets threshold {threshold.Value:C}");
                }
            }
            else if (ruleType.Contains("stop") || ruleType.Contains("allowlist"))
            {
                var blocked = GetStringList(rule.Parameters, "blocked")
                    .Concat(GetStringList(rule.Parameters, "merchants"))
                    .Concat(GetStringList(rule.Parameters, "counterparties"));
                if (blocked.Any(name => string.Equals(name, payment.CounterpartyRef, StringComparison.OrdinalIgnoreCase)))
                {
                    outcomes.Add(rule.Action == DecisionOutcome.Allow ? DecisionOutcome.Reject : rule.Action);
                    hits.Add($"{rule.RuleType}: merchant is on stop list");
                }
            }
            else if (ruleType.Contains("cumulative") || ruleType.Contains("counterparty"))
            {
                var limit = GetDecimal(rule.Parameters, "amount")
                    ?? GetDecimal(rule.Parameters, "limit");
                if (limit.HasValue)
                {
                    var total = recentPayments
                        .Where(candidate =>
                            candidate.Direction == PaymentDirection.Inbound
                            && string.Equals(candidate.CounterpartyRef, payment.CounterpartyRef, StringComparison.OrdinalIgnoreCase)
                            && candidate.EventTime >= payment.EventTime.AddDays(-30)
                            && candidate.EventTime <= payment.EventTime)
                        .Sum(candidate => candidate.Amount);
                    if (total > limit.Value)
                    {
                        outcomes.Add(rule.Action);
                        hits.Add($"{rule.RuleType}: 30-day merchant exposure {total:C} exceeds {limit.Value:C}");
                    }
                }
            }
            else if (ruleType.Contains("purpose"))
            {
                if (obligation is null)
                {
                    outcomes.Add(rule.Action);
                    hits.Add("Purpose: no matching bill or obligation");
                }
                else
                {
                    hits.Add($"Purpose: matched {obligation.SourceRef}");
                }
            }
        }

        if (outcomes.Contains(DecisionOutcome.Reject))
            return new PaymentEvaluation("Rejected", hits.Count == 0 ? ["Policy review completed"] : hits);
        if (outcomes.Contains(DecisionOutcome.Escalate))
            return new PaymentEvaluation("Escalated", hits.Count == 0 ? ["Policy review completed"] : hits);

        if (obligation is null)
        {
            hits.Add("No matching bill or obligation");
            return new PaymentEvaluation("Review", hits);
        }
        hits.Add("Matched bill and no blocking rule hit");
        return new PaymentEvaluation("Allowed", hits);
    }

    private static bool IsAchInstantEligible(PaymentEvent payment)
    {
        if (payment.Rail != Rail.ACH)
            return false;
        if (payment.RawPayload.TryGetValue("achInstantEligible", out var value))
            return value is bool eligible && eligible;
        return payment.Amount <= 25_000m
            && (payment.Memo is null
                || !payment.Memo.Contains("fallback", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsConvertedToInstant(PaymentEvent payment)
    {
        if (payment.RawPayload.TryGetValue("convertedToInstant", out var value))
            return value is bool converted && converted;
        return false;
    }

    private static decimal? GetDecimal(Dictionary<string, object> parameters, string name)
    {
        if (!parameters.TryGetValue(name, out var value))
            return null;
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var amount))
                return amount;
            if (element.ValueKind == JsonValueKind.String
                && decimal.TryParse(element.GetString(), out amount))
                return amount;
        }
        return value switch
        {
            decimal amount => amount,
            double amount => Convert.ToDecimal(amount),
            int amount => amount,
            long amount => amount,
            _ => null
        };
    }

    private static IReadOnlyList<string> GetStringList(Dictionary<string, object> parameters, string name)
    {
        if (!parameters.TryGetValue(name, out var value))
            return [];
        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
                return element.EnumerateArray()
                    .Select(item => item.GetString())
                    .Where(item => item is not null)
                    .Select(item => item!)
                    .ToList();
            if (element.ValueKind == JsonValueKind.String)
                return [element.GetString()!];
        }
        if (value is IEnumerable<string> values)
            return values.ToList();
        return [];
    }

    private sealed record PaymentEvaluation(string Outcome, IReadOnlyList<string> RuleHits);
}
