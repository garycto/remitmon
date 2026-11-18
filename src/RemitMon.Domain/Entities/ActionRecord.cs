using RemitMon.Domain.Enums;

namespace RemitMon.Domain.Entities;

public class ActionRecord : AuditableEntity
{
    public Guid DecisionId { get; set; }
    public ActionType Action { get; set; }
    public Rail Rail { get; set; }
    public ActionStatus Status { get; set; }
    public DateTime? WindowDeadline { get; set; }
    public Dictionary<string, object> Detail { get; set; } = new();
    
    public Decision? Decision { get; set; }
}
