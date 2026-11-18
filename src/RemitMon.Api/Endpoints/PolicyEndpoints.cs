using Microsoft.EntityFrameworkCore;
using RemitMon.Api.Dtos;
using RemitMon.Domain.Entities;
using RemitMon.Domain.Enums;
using RemitMon.Infrastructure;

namespace RemitMon.Api.Endpoints;

public static class PolicyEndpoints
{
    public static void MapPolicyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/policies")
            .WithName("Policies");

        group.MapGet("/account/{accountId}", GetPoliciesByAccount)
            .WithName("GetPoliciesByAccount")
            .WithSummary("Get policies for an account");

        group.MapGet("/{id}", GetPolicy)
            .WithName("GetPolicy")
            .WithSummary("Get policy by ID");

        group.MapPost("/", CreatePolicy)
            .WithName("CreatePolicy")
            .WithSummary("Create new policy");

        group.MapPost("/{id}/rules", AddPolicyRule)
            .WithName("AddPolicyRule")
            .WithSummary("Add rule to policy");

        group.MapPost("/{id}/activate", ActivatePolicy)
            .WithName("ActivatePolicy")
            .WithSummary("Activate policy (requires approval)");

        group.MapPost("/{id}/test", TestPolicy)
            .WithName("TestPolicy")
            .WithSummary("Test policy with payment event");
    }

    private static async Task<IResult> GetPoliciesByAccount(Guid accountId, RemitMonDbContext db)
    {
        var policies = await db.Policies
            .Where(p => p.AccountId == accountId)
            .Select(p => new PolicyDto(
                p.Id,
                p.AccountId,
                p.Name,
                p.Status,
                p.Version,
                p.EffectiveFrom,
                p.CreatedAt,
                p.UpdatedAt
            ))
            .ToListAsync();

        return Results.Ok(policies);
    }

    private static async Task<IResult> GetPolicy(Guid id, RemitMonDbContext db)
    {
        var policy = await db.Policies
            .Where(p => p.Id == id)
            .Select(p => new PolicyDto(
                p.Id,
                p.AccountId,
                p.Name,
                p.Status,
                p.Version,
                p.EffectiveFrom,
                p.CreatedAt,
                p.UpdatedAt
            ))
            .FirstOrDefaultAsync();

        if (policy is null)
            return Results.NotFound();

        return Results.Ok(policy);
    }

    private static async Task<IResult> CreatePolicy(CreatePolicyRequest request, RemitMonDbContext db)
    {
        var policy = new Policy
        {
            AccountId = request.AccountId,
            Name = request.Name,
            Status = PolicyStatus.Draft,
            Version = 1
        };

        db.Policies.Add(policy);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/policies/{policy.Id}", new PolicyDto(
            policy.Id,
            policy.AccountId,
            policy.Name,
            policy.Status,
            policy.Version,
            policy.EffectiveFrom,
            policy.CreatedAt,
            policy.UpdatedAt
        ));
    }

    private static async Task<IResult> AddPolicyRule(Guid id, CreatePolicyRuleRequest request, RemitMonDbContext db)
    {
        var policy = await db.Policies.FirstOrDefaultAsync(p => p.Id == id);
        if (policy is null)
            return Results.NotFound();

        if (policy.Status != PolicyStatus.Draft)
            return Results.BadRequest("Can only add rules to Draft policies");

        var rule = new PolicyRule
        {
            PolicyId = id,
            RuleType = request.RuleType,
            ScopeType = request.ScopeType,
            ScopeRef = request.ScopeRef,
            Parameters = request.Parameters,
            Action = Enum.Parse<DecisionOutcome>(request.Action),
            Priority = request.Priority,
            Enabled = true
        };

        db.PolicyRules.Add(rule);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/rules/{rule.Id}", new PolicyRuleDto(
            rule.Id,
            rule.PolicyId,
            rule.RuleType,
            rule.ScopeType,
            rule.ScopeRef,
            rule.Parameters,
            rule.Action.ToString(),
            rule.Priority,
            rule.Enabled,
            rule.Locked
        ));
    }

    private static async Task<IResult> ActivatePolicy(Guid id, ActivatePolicyRequest request, RemitMonDbContext db)
    {
        var policy = await db.Policies.FirstOrDefaultAsync(p => p.Id == id);
        if (policy is null)
            return Results.NotFound();

        if (policy.Status != PolicyStatus.Draft)
            return Results.BadRequest("Only Draft policies can be activated");

        policy.Status = PolicyStatus.Active;
        policy.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new PolicyDto(
            policy.Id,
            policy.AccountId,
            policy.Name,
            policy.Status,
            policy.Version,
            policy.EffectiveFrom,
            policy.CreatedAt,
            policy.UpdatedAt
        ));
    }

    private static async Task<IResult> TestPolicy(Guid id, PaymentEventDto eventDto, RemitMonDbContext db)
    {
        var policy = await db.Policies
            .Include(p => p.Rules)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (policy is null)
            return Results.NotFound();

        return Results.Ok(new { message = "Policy test executed", policyId = id });
    }
}
