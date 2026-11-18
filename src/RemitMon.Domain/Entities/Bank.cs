namespace RemitMon.Domain.Entities;

public class Bank : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string RoutingNumber { get; set; } = string.Empty;
    public Dictionary<string, object> Settings { get; set; } = new();
    
    public ICollection<BusinessCustomer> BusinessCustomers { get; set; } = new List<BusinessCustomer>();
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
