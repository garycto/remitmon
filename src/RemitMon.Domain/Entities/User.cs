using RemitMon.Domain.Enums;

namespace RemitMon.Domain.Entities;

public class User : AuditableEntity
{
    public string? EntraObjectId { get; set; }
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? BusinessCustomerId { get; set; }
    public Guid? BankId { get; set; }
    
    public BusinessCustomer? BusinessCustomer { get; set; }
    public Bank? Bank { get; set; }
}
