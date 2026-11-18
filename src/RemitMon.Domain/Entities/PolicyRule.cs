using RemitMon.Domain.Enums;

namespace RemitMon.Domain.Entities;

public class PolicyRule : AuditableEntity
{
    public Guid PolicyId { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string? ScopeType { get; set; }
    public string? ScopeRef { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DecisionOutcome Action { get; set; } = DecisionOutcome.Escalate;
    public int Priority { get; set; } = 0;
    public bool Enabled { get; set; } = true;
    public bool Locked { get; set; } = false;
    
    public Policy? Policy { get; set; }
}
