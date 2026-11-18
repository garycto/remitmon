using RemitMon.Domain.Enums;

namespace RemitMon.Domain.Entities;

public class BusinessCustomer : AuditableEntity
{
    public Guid BankId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public BusinessCustomerStatus Status { get; set; } = BusinessCustomerStatus.Active;
    
    public Bank? Bank { get; set; }
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Counterparty> Counterparties { get; set; } = new List<Counterparty>();
    public ICollection<ExpectedObligation> ExpectedObligations { get; set; } = new List<ExpectedObligation>();
}
