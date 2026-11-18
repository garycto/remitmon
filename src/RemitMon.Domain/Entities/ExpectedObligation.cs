using RemitMon.Domain.Enums;

namespace RemitMon.Domain.Entities;

public class ExpectedObligation : AuditableEntity
{
    public Guid BusinessCustomerId { get; set; }
    public ExpectedObligationSource Source { get; set; }
    public string SourceRef { get; set; } = string.Empty;
    public Guid? CounterpartyId { get; set; }
    public string CounterpartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public ExpectedObligationStatus Status { get; set; } = ExpectedObligationStatus.Open;
    public DateTime CreatedEventAt { get; set; }
    public string? CreatedByExt { get; set; }
    public Guid? MatchedPaymentEventId { get; set; }
    
    public BusinessCustomer? BusinessCustomer { get; set; }
    public Counterparty? Counterparty { get; set; }
    public PaymentEvent? MatchedPaymentEvent { get; set; }
}
