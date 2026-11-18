using RemitMon.Domain.Enums;

namespace RemitMon.Api.Dtos;

public record PolicyDto(
    Guid Id,
    Guid AccountId,
    string Name,
    PolicyStatus Status,
    int Version,
    DateTime EffectiveFrom,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreatePolicyRequest(
    Guid AccountId,
    string Name
);

public record ActivatePolicyRequest(
    string Note
);

public record PolicyRuleDto(
    Guid Id,
    Guid PolicyId,
    string RuleType,
    string? ScopeType,
    string? ScopeRef,
    Dictionary<string, object> Parameters,
    string Action,
    int Priority,
    bool Enabled,
    bool Locked
);

public record CreatePolicyRuleRequest(
    string RuleType,
    string? ScopeType,
    string? ScopeRef,
    Dictionary<string, object> Parameters,
    string Action,
    int Priority
);
