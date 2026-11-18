namespace RemitMon.Domain.Entities;

public class CounterpartyChangeLog : AuditableEntity
{
    public Guid CounterpartyId { get; set; }
    public string Field { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    
    public Counterparty? Counterparty { get; set; }
}
