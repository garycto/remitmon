using RemitMon.Domain.Entities;
using RemitMon.Domain.Enums;

namespace RemitMon.Infrastructure.Seeding;

public class DemoDataSeeder
{
    public static void SeedDemoData(RemitMonDbContext context)
    {
        // Don't seed if data already exists
        if (context.Banks.Any())
            return;

        // Create banks
        var bankUS = new Bank
        {
            Id = Guid.NewGuid(),
            Name = "US Bank",
            RoutingNumber = "111000025",
            Settings = new Dictionary<string, object> { { "country", "US" } },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var bankUK = new Bank
        {
            Id = Guid.NewGuid(),
            Name = "HSBC UK",
            RoutingNumber = "403000",
            Settings = new Dictionary<string, object> { { "country", "GB" } },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Banks.AddRange(bankUS, bankUK);
        context.SaveChanges();

        // Create business customers
        var customer1 = new BusinessCustomer
        {
            Id = Guid.NewGuid(),
            BankId = bankUS.Id,
            LegalName = "Acme Corp USA",
            Status = BusinessCustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var customer2 = new BusinessCustomer
        {
            Id = Guid.NewGuid(),
            BankId = bankUK.Id,
            LegalName = "Global Finance Ltd",
            Status = BusinessCustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.BusinessCustomers.AddRange(customer1, customer2);
        context.SaveChanges();

        // Create accounts
        var account1 = new Account
        {
            Id = Guid.NewGuid(),
            BusinessCustomerId = customer1.Id,
            BankId = bankUS.Id,
            DisplayName = "USD Operating Account",
            MaskedAccountNumber = "****1234",
            Mode = AccountMode.Monitor,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var account2 = new Account
        {
            Id = Guid.NewGuid(),
            BusinessCustomerId = customer1.Id,
            BankId = bankUS.Id,
            DisplayName = "EUR International Payments",
            MaskedAccountNumber = "****5678",
            Mode = AccountMode.Enforce,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var account3 = new Account
        {
            Id = Guid.NewGuid(),
            BusinessCustomerId = customer2.Id,
            BankId = bankUK.Id,
            DisplayName = "GBP Treasury Account",
            MaskedAccountNumber = "****9012",
            Mode = AccountMode.Monitor,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Accounts.AddRange(account1, account2, account3);
        context.SaveChanges();

        // Create policies
        var policy1 = new Policy
        {
            Id = Guid.NewGuid(),
            AccountId = account1.Id,
            Name = "High-Value Transaction Alert",
            Status = PolicyStatus.Active,
            Version = 1,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow
        };

        var policy2 = new Policy
        {
            Id = Guid.NewGuid(),
            AccountId = account2.Id,
            Name = "OFAC Compliance Check",
            Status = PolicyStatus.Active,
            Version = 1,
            EffectiveFrom = DateTime.UtcNow.AddDays(-15),
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            UpdatedAt = DateTime.UtcNow
        };

        var policy3 = new Policy
        {
            Id = Guid.NewGuid(),
            AccountId = account3.Id,
            Name = "Unusual Activity Detection",
            Status = PolicyStatus.Draft,
            Version = 1,
            EffectiveFrom = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Policies.AddRange(policy1, policy2, policy3);
        context.SaveChanges();

        // Create rules for policies
        var rule1 = new PolicyRule
        {
            Id = Guid.NewGuid(),
            PolicyId = policy1.Id,
            RuleType = "HighValueAlert",
            Parameters = new Dictionary<string, object> { { "threshold", 50000 } },
            Action = DecisionOutcome.Escalate,
            Priority = 1,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var rule2 = new PolicyRule
        {
            Id = Guid.NewGuid(),
            PolicyId = policy1.Id,
            RuleType = "HighValueAlert",
            Parameters = new Dictionary<string, object> { { "threshold", 100000 } },
            Action = DecisionOutcome.Reject,
            Priority = 2,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var rule3 = new PolicyRule
        {
            Id = Guid.NewGuid(),
            PolicyId = policy2.Id,
            RuleType = "SanctionedCountryCheck",
            Parameters = new Dictionary<string, object> { { "countries", new[] { "IR", "KP", "SY" } } },
            Action = DecisionOutcome.Reject,
            Priority = 1,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.PolicyRules.AddRange(rule1, rule2, rule3);
        context.SaveChanges();

        // Create counterparties
        var cp1 = new Counterparty
        {
            Id = Guid.NewGuid(),
            BusinessCustomerId = customer1.Id,
            Name = "Partner Bank Switzerland",
            Status = CounterpartyStatus.Approved,
            Source = CounterpartySource.Manual,
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var cp2 = new Counterparty
        {
            Id = Guid.NewGuid(),
            BusinessCustomerId = customer1.Id,
            Name = "Emerging Markets Corp",
            Status = CounterpartyStatus.Pending,
            Source = CounterpartySource.Manual,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var cp3 = new Counterparty
        {
            Id = Guid.NewGuid(),
            BusinessCustomerId = customer2.Id,
            Name = "Tokyo Financial Partners",
            Status = CounterpartyStatus.Approved,
            Source = CounterpartySource.Manual,
            CreatedAt = DateTime.UtcNow.AddDays(-45),
            UpdatedAt = DateTime.UtcNow.AddDays(-20)
        };

        context.Counterparties.AddRange(cp1, cp2, cp3);
        context.SaveChanges();
    }
}
