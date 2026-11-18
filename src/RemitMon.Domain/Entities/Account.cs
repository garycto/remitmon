using RemitMon.Domain.Enums;

namespace RemitMon.Domain.Entities;

public class Account : AuditableEntity
{
    public Guid BusinessCustomerId { get; set; }
    public Guid BankId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string MaskedAccountNumber { get; set; } = string.Empty;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public AccountMode Mode { get; set; } = AccountMode.Monitor;
    
    public BusinessCustomer? BusinessCustomer { get; set; }
    public Bank? Bank { get; set; }
    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    public ICollection<PaymentEvent> PaymentEvents { get; set; } = new List<PaymentEvent>();
}
