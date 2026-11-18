namespace RemitMon.Domain.Entities;

public class AuditEntry : AuditableEntity
{
    public string Actor { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object>? Before { get; set; }
    public Dictionary<string, object>? After { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? PrevHash { get; set; }
    public string? Hash { get; set; }
}
