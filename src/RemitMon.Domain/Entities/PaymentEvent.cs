using RemitMon.Domain.Enums;

namespace RemitMon.Domain.Entities;

public class PaymentEvent : AuditableEntity
{
    public Guid AccountId { get; set; }
    public string? ExternalEventId { get; set; }
    public Rail Rail { get; set; }
    public PaymentDirection Direction { get; set; }
    public PaymentInitiation Initiation { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? CounterpartyRef { get; set; }
    public Guid? CounterpartyId { get; set; }
    public string? AchCompanyId { get; set; }
    public string? AchSecCode { get; set; }
    public string? Memo { get; set; }
    public string? Reference { get; set; }
    public string? InvoiceRef { get; set; }
    public DateTime EventTime { get; set; }
    public DateTime? SettlementDate { get; set; }
    public Dictionary<string, object> RawPayload { get; set; } = new();
    public string? CorrelationId { get; set; }
    
    public Account? Account { get; set; }
    public Counterparty? Counterparty { get; set; }
    public ICollection<Decision> Decisions { get; set; } = new List<Decision>();
}
