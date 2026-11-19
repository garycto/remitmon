using Microsoft.EntityFrameworkCore;
using RemitMon.Domain.Entities;
using RemitMon.Domain.Enums;
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
    public DbSet<Approval> Approvals => Set<Approval>();
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

        modelBuilder.Entity<PaymentEvent>()
            .HasIndex(pe => pe.ExternalEventId)
            .IsUnique();

        modelBuilder.Entity<PaymentEvent>()
            .Property(pe => pe.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ExpectedObligation>()
            .Property(eo => eo.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Decision>()
            .HasMany(d => d.Approvals)
            .WithOne(approval => approval.Decision)
            .HasForeignKey(approval => approval.DecisionId);

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
    }

    private static string ConvertToSnakeCase(string input)
    {
        return string.Concat(input.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString().ToLower() : x.ToString().ToLower()));
    }
}
