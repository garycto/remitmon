using RemitMon.Domain.Enums;

namespace RemitMon.Domain.Entities;

public class Decision : AuditableEntity
{
    public Guid PaymentEventId { get; set; }
    public Guid PolicyId { get; set; }
    public int PolicyVersion { get; set; }
    public DecisionOutcome Outcome { get; set; }
    public List<Dictionary<string, object>> Reasons { get; set; } = new();
    public DateTime EvaluatedAt { get; set; }
    public int LatencyMs { get; set; }
    public AccountMode ModeApplied { get; set; }
    public string? ActionTaken { get; set; }
    public string? ActionStatus { get; set; }
    
    public PaymentEvent? PaymentEvent { get; set; }
    public Policy? Policy { get; set; }
    public ICollection<Exception> Exceptions { get; set; } = new List<Exception>();
    public ICollection<ActionRecord> Actions { get; set; } = new List<ActionRecord>();
}
