using Microsoft.EntityFrameworkCore;
using RemitMon.Domain.Entities;
using RemitMon.Domain.Enums;
using Exception = RemitMon.Domain.Entities.Exception;

namespace RemitMon.Infrastructure;

public class RemitMonDbContext : DbContext
{
    public RemitMonDbContext(DbContextOptions<RemitMonDbContext> options) : base(options) { }

    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<BusinessCustomer> BusinessCustomers => Set<BusinessCustomer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Counterparty> Counterparties => Set<Counterparty>();
    public DbSet<CounterpartyChangeLog> CounterpartyChangeLogs => Set<CounterpartyChangeLog>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<PolicyRule> PolicyRules => Set<PolicyRule>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<ExpectedObligation> ExpectedObligations => Set<ExpectedObligation>();
    public DbSet<Decision> Decisions => Set<Decision>();
    public DbSet<Exception> Exceptions => Set<Exception>();
    public DbSet<ActionRecord> ActionRecords => Set<ActionRecord>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Convert table names to snake_case
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ConvertToSnakeCase(entity.GetTableName()!));
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ConvertToSnakeCase(property.Name));
            }
        }

        // Configure entity keys and relationships
        modelBuilder.Entity<Bank>()
            .HasMany(b => b.BusinessCustomers)
            .WithOne(bc => bc.Bank)
            .HasForeignKey(bc => bc.BankId);

        modelBuilder.Entity<BusinessCustomer>()
            .HasMany(bc => bc.Accounts)
            .WithOne(a => a.BusinessCustomer)
            .HasForeignKey(a => a.BusinessCustomerId);

        modelBuilder.Entity<BusinessCustomer>()
            .HasMany(bc => bc.Users)
            .WithOne(u => u.BusinessCustomer)
            .HasForeignKey(u => u.BusinessCustomerId);

        modelBuilder.Entity<Account>()
            .HasMany(a => a.Policies)
            .WithOne(p => p.Account)
            .HasForeignKey(p => p.AccountId);

        modelBuilder.Entity<Account>()
            .HasMany(a => a.PaymentEvents)
            .WithOne(pe => pe.Account)
            .HasForeignKey(pe => pe.AccountId);

        modelBuilder.Entity<PolicyRule>()
            .HasOne(pr => pr.Policy)
            .WithMany(p => p.Rules)
            .HasForeignKey(pr => pr.PolicyId);

        // Configure PolicyRule.Parameters as JSON
        modelBuilder.Entity<PolicyRule>()
            .Property(pr => pr.Parameters)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Policy>()
            .HasMany(p => p.Decisions)
            .WithOne(d => d.Policy)
            .HasForeignKey(d => d.PolicyId);

        modelBuilder.Entity<PaymentEvent>()
            .HasMany(pe => pe.Decisions)
            .WithOne(d => d.PaymentEvent)
            .HasForeignKey(d => d.PaymentEventId);

        modelBuilder.Entity<Decision>()
            .HasMany(d => d.Exceptions)
            .WithOne(e => e.Decision)
            .HasForeignKey(e => e.DecisionId);

        modelBuilder.Entity<ActionRecord>()
            .HasOne(ar => ar.Decision)
            .WithMany(d => d.Actions)
            .HasForeignKey(ar => ar.DecisionId);

        // Configure ActionRecord.Detail as JSON
        modelBuilder.Entity<ActionRecord>()
            .Property(ar => ar.Detail)
            .HasColumnType("jsonb");

        // Configure AuditEntry JSON properties
        modelBuilder.Entity<AuditEntry>()
            .Property(ae => ae.Before)
            .HasColumnType("jsonb");

        modelBuilder.Entity<AuditEntry>()
            .Property(ae => ae.After)
            .HasColumnType("jsonb");

        // Configure Bank.Settings as JSON
        modelBuilder.Entity<Bank>()
            .Property(b => b.Settings)
            .HasColumnType("jsonb");

        // Configure PaymentEvent.RawPayload as JSON
        modelBuilder.Entity<PaymentEvent>()
            .Property(pe => pe.RawPayload)
            .HasColumnType("jsonb");

        // Configure Decision.Reasons as JSON
        modelBuilder.Entity<Decision>()
            .Property(d => d.Reasons)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Counterparty>()
            .HasMany(c => c.ChangeLogs)
            .WithOne(cl => cl.Counterparty)
            .HasForeignKey(cl => cl.CounterpartyId);

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;

        // Banks
        var bank1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var bank2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<Bank>().HasData(
            new Bank { Id = bank1Id, Name = "FirstBank", RoutingNumber = "021000021", CreatedAt = now, UpdatedAt = now },
            new Bank { Id = bank2Id, Name = "SecondBank", RoutingNumber = "091000019", CreatedAt = now, UpdatedAt = now }
        );

        // Business Customers
        var customer1Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var customer2Id = Guid.Parse("44444444-4444-4444-4444-444444444444");

        modelBuilder.Entity<BusinessCustomer>().HasData(
            new BusinessCustomer
            {
                Id = customer1Id,
                BankId = bank1Id,
                LegalName = "ACME Corporation",
                Status = BusinessCustomerStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            },
            new BusinessCustomer
            {
                Id = customer2Id,
                BankId = bank1Id,
                LegalName = "TechStartup Inc",
                Status = BusinessCustomerStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        // Accounts
        var account1Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var account2Id = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var account3Id = Guid.Parse("77777777-7777-7777-7777-777777777777");

        modelBuilder.Entity<Account>().HasData(
            new Account
            {
                Id = account1Id,
                BusinessCustomerId = customer1Id,
                BankId = bank1Id,
                DisplayName = "Operating Account",
                MaskedAccountNumber = "****1234",
                Status = AccountStatus.Active,
                Mode = AccountMode.Monitor,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Account
            {
                Id = account2Id,
                BusinessCustomerId = customer1Id,
                BankId = bank1Id,
                DisplayName = "Payroll Account",
                MaskedAccountNumber = "****5678",
                Status = AccountStatus.Active,
                Mode = AccountMode.Enforce,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Account
            {
                Id = account3Id,
                BusinessCustomerId = customer2Id,
                BankId = bank1Id,
                DisplayName = "Main Account",
                MaskedAccountNumber = "****9012",
                Status = AccountStatus.Active,
                Mode = AccountMode.Monitor,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        // Users
        var user1Id = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var user2Id = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var user3Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var user4Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = user1Id,
                Email = "admin@acme.com",
                Role = UserRole.BusinessAdmin,
                BusinessCustomerId = customer1Id,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                Id = user2Id,
                Email = "approver@acme.com",
                Role = UserRole.BusinessApprover,
                BusinessCustomerId = customer1Id,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                Id = user3Id,
                Email = "bank@firstbank.com",
                Role = UserRole.BankAdmin,
                BankId = bank1Id,
                CreatedAt = now,
                UpdatedAt = now
            },
            new User
            {
                Id = user4Id,
                Email = "platform@remitmon.com",
                Role = UserRole.PlatformAdmin,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        // Counterparties
        var counterparty1Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var counterparty2Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        modelBuilder.Entity<Counterparty>().HasData(
            new Counterparty
            {
                Id = counterparty1Id,
                BusinessCustomerId = customer1Id,
                Name = "ABC Supplies Inc",
                Status = CounterpartyStatus.Approved,
                Source = CounterpartySource.Manual,
                AchCompanyIds = new List<string> { "1234567890" },
                CreatedAt = now,
                UpdatedAt = now
            },
            new Counterparty
            {
                Id = counterparty2Id,
                BusinessCustomerId = customer1Id,
                Name = "XYZ Logistics",
                Status = CounterpartyStatus.Pending,
                Source = CounterpartySource.Observed,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        // Policies
        var policy1Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        modelBuilder.Entity<Policy>().HasData(
            new Policy
            {
                Id = policy1Id,
                AccountId = account1Id,
                Name = "Default Policy",
                Status = PolicyStatus.Draft,
                Version = 1,
                EffectiveFrom = now,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        // Policy Rules
        var rule1Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var rule2Id = Guid.Parse("11111112-1111-1111-1111-111111111111");

        modelBuilder.Entity<PolicyRule>().HasData(
            new PolicyRule
            {
                Id = rule1Id,
                PolicyId = policy1Id,
                RuleType = "MaxAmountPerTransaction",
                ScopeType = "Account",
                Action = DecisionOutcome.Reject,
                Parameters = new Dictionary<string, object> { { "amount", 50000m } },
                Priority = 1,
                Enabled = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new PolicyRule
            {
                Id = rule2Id,
                PolicyId = policy1Id,
                RuleType = "PaymentTypeAllowed",
                ScopeType = "Account",
                Action = DecisionOutcome.Allow,
                Parameters = new Dictionary<string, object>
                {
                    { "rails", new[] { "ACH", "RTP" } },
                    { "initiations", new[] { "Push" } }
                },
                Priority = 2,
                Enabled = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        );
    }

    private static string ConvertToSnakeCase(string input)
    {
        return string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString().ToLower() : x.ToString().ToLower()));
    }
}
