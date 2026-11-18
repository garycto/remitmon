using RemitMon.Domain.Enums;

namespace RemitMon.Domain.Entities;

public class Policy : AuditableEntity
{
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PolicyStatus Status { get; set; } = PolicyStatus.Draft;
    public int Version { get; set; } = 1;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    
    public Account? Account { get; set; }
    public ICollection<PolicyRule> Rules { get; set; } = new List<PolicyRule>();
    public ICollection<Decision> Decisions { get; set; } = new List<Decision>();
}
