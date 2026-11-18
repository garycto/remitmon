using RemitMon.Domain.Enums;

namespace RemitMon.Domain.Entities;

public class Counterparty : AuditableEntity
{
    public Guid BusinessCustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CounterpartyStatus Status { get; set; } = CounterpartyStatus.Pending;
    public CounterpartySource Source { get; set; } = CounterpartySource.Manual;
    public List<string> AchCompanyIds { get; set; } = new();
    public List<string> RoutingAccountFingerprints { get; set; } = new();
    public string? TaxIdHash { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public BusinessCustomer? BusinessCustomer { get; set; }
    public ICollection<CounterpartyChangeLog> ChangeLogs { get; set; } = new List<CounterpartyChangeLog>();
}
