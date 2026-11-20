using RemitMon.Domain.Entities;
using RemitMon.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace RemitMon.Infrastructure.Seeding;

public class DemoDataSeeder
{
    public static void SeedDemoData(RemitMonDbContext context)
    {
        if (context.Banks.Any())
        {
            SeedFinancialActivity(context);
            return;
        }

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

        var rule4 = new PolicyRule
        {
            Id = Guid.NewGuid(),
            PolicyId = policy1.Id,
            RuleType = "CounterpartyAllowlist",
            Parameters = new Dictionary<string, object>
            {
                { "blocked", new[] { "East Coast Logistics" } }
            },
            Action = DecisionOutcome.Reject,
            Priority = 3,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var rule5 = new PolicyRule
        {
            Id = Guid.NewGuid(),
            PolicyId = policy1.Id,
            RuleType = "CumulativeExposure",
            Parameters = new Dictionary<string, object>
            {
                { "amount", 15000 },
                { "period", "30d" }
            },
            Action = DecisionOutcome.Escalate,
            Priority = 4,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.PolicyRules.AddRange(rule1, rule2, rule3, rule4, rule5);
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

        SeedFinancialActivity(context);
    }

    private static void SeedFinancialActivity(RemitMonDbContext context)
    {
        var usdAccount = context.Accounts
            .SingleOrDefault(a => a.DisplayName == "USD Operating Account"
                || a.DisplayName == "Northstar Components — Operating");
        var eurAccount = context.Accounts
            .SingleOrDefault(a => a.DisplayName == "EUR International Payments"
                || a.DisplayName == "Apex Facilities — International");
        var gbpAccount = context.Accounts
            .SingleOrDefault(a => a.DisplayName == "GBP Treasury Account"
                || a.DisplayName == "Meridian Supply — Treasury");

        if (usdAccount is not null)
            usdAccount.DisplayName = "Northstar Components — Operating";
        if (eurAccount is not null)
            eurAccount.DisplayName = "Apex Facilities — International";
        if (gbpAccount is not null)
            gbpAccount.DisplayName = "Meridian Supply — Treasury";

        var existingCustomers = context.BusinessCustomers.ToList();
        if (existingCustomers.Count > 0)
        {
            existingCustomers[0].LegalName = "Northstar Components";
            if (existingCustomers.Count > 1)
                existingCustomers[1].LegalName = "Apex Facilities Group";
        }
        if (!existingCustomers.Any(customer => customer.LegalName == "Meridian Supply Co."))
        {
            var bank = context.Banks.OrderBy(candidate => candidate.Name).First();
            existingCustomers.Add(new BusinessCustomer
            {
                Id = Guid.NewGuid(),
                BankId = bank.Id,
                LegalName = "Meridian Supply Co.",
                Status = BusinessCustomerStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            context.BusinessCustomers.Add(existingCustomers[^1]);
        }
        context.SaveChanges();

        if (usdAccount is null || eurAccount is null || gbpAccount is null)
        {
            throw new InvalidOperationException(
                "Demo payment activity requires the three demo accounts to exist.");
        }

        var now = DateTime.UtcNow;
        var usdPolicy = context.Policies
            .Include(policy => policy.Rules)
            .FirstOrDefault(policy =>
                policy.AccountId == usdAccount.Id
                && policy.Status == PolicyStatus.Active);
        if (usdPolicy is not null)
        {
            if (!usdPolicy.Rules.Any(rule => rule.RuleType == "CounterpartyAllowlist"))
            {
                context.PolicyRules.Add(new PolicyRule
                {
                    PolicyId = usdPolicy.Id,
                    RuleType = "CounterpartyAllowlist",
                    Parameters = new Dictionary<string, object>
                    {
                        { "blocked", new[] { "East Coast Logistics" } }
                    },
                    Action = DecisionOutcome.Reject,
                    Priority = 3,
                    Enabled = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            if (!usdPolicy.Rules.Any(rule => rule.RuleType == "CumulativeExposure"))
            {
                context.PolicyRules.Add(new PolicyRule
                {
                    PolicyId = usdPolicy.Id,
                    RuleType = "CumulativeExposure",
                    Parameters = new Dictionary<string, object>
                    {
                        { "amount", 15000 },
                        { "period", "30d" }
                    },
                    Action = DecisionOutcome.Escalate,
                    Priority = 4,
                    Enabled = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            context.SaveChanges();
        }
        var payments = new[]
        {
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.ACH,
                Direction = PaymentDirection.Inbound,
                Initiation = PaymentInitiation.Pull,
                Amount = 42_100m,
                Currency = "USD",
                CounterpartyRef = "East Coast Logistics",
                AchCompanyId = "ECOAST01",
                AchSecCode = "CCD",
                Memo = "Stop-list review",
                Reference = "DEMO-PAY-IN-STOPLIST-001",
                EventTime = now.AddMinutes(-45),
                SettlementDate = now.AddDays(1)
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.ACH,
                Direction = PaymentDirection.Inbound,
                Initiation = PaymentInitiation.Pull,
                Amount = 8_500m,
                Currency = "USD",
                CounterpartyRef = "Northstar Components",
                AchCompanyId = "NORTHSTAR01",
                AchSecCode = "CCD",
                Memo = "Cumulative limit review",
                Reference = "DEMO-PAY-IN-CUMULATIVE-001",
                InvoiceRef = "DEMO-BILL-2026-001",
                EventTime = now.AddMinutes(-20),
                SettlementDate = now.AddDays(1)
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.ACH,
                Direction = PaymentDirection.Inbound,
                Initiation = PaymentInitiation.Push,
                Amount = 21_450m,
                Currency = "USD",
                CounterpartyRef = "Alpine Distribution",
                AchCompanyId = "ALPINE01",
                AchSecCode = "CCD",
                Memo = "Wholesale settlement",
                Reference = "DEMO-PAY-IN-ACH-001",
                InvoiceRef = "INV-AD-1048",
                EventTime = now.AddDays(-2),
                SettlementDate = now.AddDays(-1)
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.RTP,
                Direction = PaymentDirection.Inbound,
                Initiation = PaymentInitiation.RfP,
                Amount = 8_250m,
                Currency = "USD",
                CounterpartyRef = "Northstar Components",
                Memo = "Request for payment - invoice NS-1092",
                Reference = "DEMO-RFP-IN-RTP-001",
                InvoiceRef = "NS-1092",
                EventTime = now.AddHours(-2)
            },
            new PaymentEvent
            {
                AccountId = eurAccount.Id,
                Rail = Rail.FedNow,
                Direction = PaymentDirection.Inbound,
                Initiation = PaymentInitiation.RfP,
                Amount = 3_275m,
                Currency = "USD",
                CounterpartyRef = "Apex Hosting",
                Memo = "Request for payment - hosting renewal",
                Reference = "DEMO-RFP-IN-FEDNOW-001",
                InvoiceRef = "APEX-2026-3321",
                EventTime = now.AddHours(-5)
            },
            new PaymentEvent
            {
                AccountId = gbpAccount.Id,
                Rail = Rail.Wire,
                Direction = PaymentDirection.Inbound,
                Initiation = PaymentInitiation.Push,
                Amount = 68_500m,
                Currency = "GBP",
                CounterpartyRef = "Global Finance Ltd",
                Memo = "Treasury funding",
                Reference = "DEMO-PAY-IN-WIRE-001",
                EventTime = now.AddDays(-1),
                SettlementDate = now.AddHours(-12)
            },
            new PaymentEvent
            {
                AccountId = eurAccount.Id,
                Rail = Rail.ACH,
                Direction = PaymentDirection.Inbound,
                Initiation = PaymentInitiation.Pull,
                Amount = 3_420m,
                Currency = "EUR",
                CounterpartyRef = "Partner Bank Switzerland",
                AchCompanyId = "PARTNER01",
                AchSecCode = "CCD",
                Memo = "Scheduled collection",
                Reference = "DEMO-PAY-IN-PULL-001",
                EventTime = now.AddHours(-8),
                SettlementDate = now.AddDays(1)
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.ACH,
                Direction = PaymentDirection.Outbound,
                Initiation = PaymentInitiation.Push,
                Amount = 9_300m,
                Currency = "USD",
                CounterpartyRef = "Meridian Payroll",
                AchCompanyId = "MERIDIAN01",
                AchSecCode = "CCD",
                Memo = "Weekly payroll",
                Reference = "DEMO-PAY-OUT-ACH-001",
                InvoiceRef = "DEMO-BILL-2026-002",
                EventTime = now.AddMinutes(-30),
                SettlementDate = now.AddDays(1)
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.RTP,
                Direction = PaymentDirection.Outbound,
                Initiation = PaymentInitiation.Push,
                Amount = 18_750m,
                Currency = "USD",
                CounterpartyRef = "Coastal Freight",
                Memo = "Freight invoice payment",
                Reference = "DEMO-PAY-OUT-RTP-001",
                InvoiceRef = "DEMO-BILL-2026-004",
                EventTime = now.AddHours(-4),
                SettlementDate = now
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.Wire,
                Direction = PaymentDirection.Outbound,
                Initiation = PaymentInitiation.Push,
                Amount = 24_600m,
                Currency = "USD",
                CounterpartyRef = "Westlake Services",
                Memo = "Monthly services invoice",
                Reference = "DEMO-PAY-OUT-WIRE-001",
                InvoiceRef = "DEMO-BILL-2026-005",
                EventTime = now.AddDays(-15),
                SettlementDate = now.AddDays(-14)
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.FedNow,
                Direction = PaymentDirection.Outbound,
                Initiation = PaymentInitiation.Push,
                Amount = 4_850m,
                Currency = "USD",
                CounterpartyRef = "Cloudline Utilities",
                Memo = "Utility bill",
                Reference = "DEMO-PAY-OUT-FEDNOW-001",
                InvoiceRef = "CLOUDLINE-1044",
                EventTime = now.AddHours(-1),
                SettlementDate = now
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.ACH,
                Direction = PaymentDirection.Outbound,
                Initiation = PaymentInitiation.Push,
                Amount = 6_750m,
                Currency = "USD",
                CounterpartyRef = "Northstar Components",
                AchCompanyId = "NORTHSTAR02",
                AchSecCode = "CCD",
                Memo = "Instant attempt fallback to ACH - recipient not reachable",
                Reference = "DEMO-PAY-FALLBACK-001",
                EventTime = now.AddDays(-3),
                SettlementDate = now.AddDays(-1)
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.RTP,
                Direction = PaymentDirection.Outbound,
                Initiation = PaymentInitiation.Push,
                Amount = 11_200m,
                Currency = "USD",
                CounterpartyRef = "Apex Facilities",
                Memo = "Supplier payment",
                Reference = "DEMO-PAY-OUT-RTP-002",
                EventTime = now.AddDays(-5),
                SettlementDate = now.AddDays(-5)
            },
            new PaymentEvent
            {
                AccountId = eurAccount.Id,
                Rail = Rail.FedNow,
                Direction = PaymentDirection.Inbound,
                Initiation = PaymentInitiation.RfP,
                Amount = 5_940m,
                Currency = "USD",
                CounterpartyRef = "Apex Hosting",
                Memo = "Equipment lease",
                Reference = "DEMO-PAY-IN-FEDNOW-002",
                EventTime = now.AddDays(-7)
            },
            new PaymentEvent
            {
                AccountId = gbpAccount.Id,
                Rail = Rail.Wire,
                Direction = PaymentDirection.Outbound,
                Initiation = PaymentInitiation.Push,
                Amount = 92_000m,
                Currency = "GBP",
                CounterpartyRef = "Global Treasury Services",
                Memo = "International settlement",
                Reference = "DEMO-PAY-OUT-WIRE-002",
                EventTime = now.AddDays(-9),
                SettlementDate = now.AddDays(-8)
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.ACH,
                Direction = PaymentDirection.Outbound,
                Initiation = PaymentInitiation.Push,
                Amount = 14_500m,
                Currency = "USD",
                CounterpartyRef = "Meridian Supply",
                AchCompanyId = "MERIDIAN02",
                AchSecCode = "CCD",
                Memo = "Vendor settlement",
                Reference = "DEMO-PAY-OUT-ACH-002",
                EventTime = now.AddDays(-12),
                SettlementDate = now.AddDays(-10)
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.RTP,
                Direction = PaymentDirection.Outbound,
                Initiation = PaymentInitiation.Push,
                Amount = 7_800m,
                Currency = "USD",
                CounterpartyRef = "Northstar Components",
                Memo = "Invoice settlement",
                Reference = "DEMO-PAY-OUT-RTP-003",
                EventTime = now.AddDays(-18),
                SettlementDate = now.AddDays(-18)
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.Wire,
                Direction = PaymentDirection.Outbound,
                Initiation = PaymentInitiation.Push,
                Amount = 36_250m,
                Currency = "USD",
                CounterpartyRef = "Apex Facilities",
                Memo = "Construction funding",
                Reference = "DEMO-PAY-OUT-WIRE-003",
                EventTime = now.AddDays(-21),
                SettlementDate = now.AddDays(-20)
            },
            new PaymentEvent
            {
                AccountId = eurAccount.Id,
                Rail = Rail.ACH,
                Direction = PaymentDirection.Inbound,
                Initiation = PaymentInitiation.Pull,
                Amount = 12_350m,
                Currency = "USD",
                CounterpartyRef = "Alpine Distribution",
                AchCompanyId = "ALPINE02",
                AchSecCode = "CCD",
                Memo = "Customer receivable",
                Reference = "DEMO-PAY-IN-ACH-002",
                EventTime = now.AddDays(-24),
                SettlementDate = now.AddDays(-22)
            },
            new PaymentEvent
            {
                AccountId = usdAccount.Id,
                Rail = Rail.FedNow,
                Direction = PaymentDirection.Outbound,
                Initiation = PaymentInitiation.Push,
                Amount = 9_600m,
                Currency = "USD",
                CounterpartyRef = "Meridian Supply",
                Memo = "Expedited vendor payment",
                Reference = "DEMO-PAY-OUT-FEDNOW-002",
                EventTime = now.AddDays(-27),
                SettlementDate = now.AddDays(-27)
            }
        };

        var existingPaymentReferences = context.PaymentEvents
            .Where(payment => payment.Reference != null && payment.Reference.StartsWith("DEMO-"))
            .Select(payment => payment.Reference!)
            .ToHashSet();

        foreach (var payment in payments.Where(payment =>
                     !existingPaymentReferences.Contains(payment.Reference!)
                     && payment.Rail == Rail.ACH))
        {
            var fallback = payment.Memo?.Contains("fallback", StringComparison.OrdinalIgnoreCase) == true;
            var referenceEligible = payment.Reference != null
                && Math.Abs(payment.Reference.GetHashCode()) % 3 != 0;
            payment.RawPayload["achInstantEligible"] = !fallback
                && referenceEligible
                && payment.Amount <= 25_000m;
            payment.RawPayload["convertedToInstant"] = false;
        }

        context.PaymentEvents.AddRange(
            payments.Where(payment => !existingPaymentReferences.Contains(payment.Reference!)));
        context.SaveChanges();

        var generatedReferences = context.PaymentEvents
            .Where(payment => payment.Reference != null && payment.Reference.StartsWith("DEMO-VOLUME-"))
            .Select(payment => payment.Reference!)
            .ToHashSet();
        var accounts = new[] { usdAccount, eurAccount, gbpAccount };
        var rails = new[] { Rail.ACH, Rail.RTP, Rail.FedNow, Rail.Wire };
        var customerNames = new[]
        {
            "Northstar Components",
            "Apex Facilities Group",
            "Meridian Supply Co.",
            "Alpine Distribution",
            "Coastal Freight",
            "Westlake Services"
        };
        var generatedPayments = new List<PaymentEvent>();

        for (var day = 0; day < 30; day++)
        {
            for (var item = 0; item < 8; item++)
            {
                var reference = $"DEMO-VOLUME-{day:D2}-{item:D2}";
                if (generatedReferences.Contains(reference))
                    continue;

                var account = accounts[(day + item) % accounts.Length];
                var rail = rails[(day * 3 + item) % rails.Length];
                var direction = (day + item) % 5 == 0
                    ? PaymentDirection.Inbound
                    : PaymentDirection.Outbound;
                var amount = 1850m + ((day * 997 + item * 437) % 48000);
                var fallback = rail == Rail.ACH && day % 11 == 0 && item == 3;

                generatedPayments.Add(new PaymentEvent
                {
                    AccountId = account.Id,
                    ExternalEventId = fallback ? $"fallback-{reference}" : null,
                    Rail = rail,
                    Direction = direction,
                    Initiation = direction == PaymentDirection.Inbound
                        ? (rail == Rail.ACH ? PaymentInitiation.Pull : PaymentInitiation.RfP)
                        : PaymentInitiation.Push,
                    Amount = amount,
                    Currency = account.Id == gbpAccount.Id ? "GBP" : "USD",
                    CounterpartyRef = customerNames[(day + item) % customerNames.Length],
                    AchCompanyId = rail == Rail.ACH ? $"DEMO{day:D2}{item:D2}" : null,
                    AchSecCode = rail == Rail.ACH ? "CCD" : null,
                    Memo = fallback
                        ? "Instant attempt fallback to ACH - recipient not reachable"
                        : rail == Rail.Wire ? "Treasury wire settlement"
                        : rail == Rail.FedNow ? "Real-time customer payment"
                        : rail == Rail.RTP ? "RTP supplier payment"
                        : "ACH customer payment",
                    Reference = reference,
                    EventTime = now.AddDays(-day).AddMinutes(item * 37),
                    SettlementDate = rail == Rail.ACH
                        ? now.AddDays(-day).AddDays(1)
                        : now.AddDays(-day)
                });
                if (rail == Rail.ACH)
                {
                    generatedPayments[^1].RawPayload["achInstantEligible"] =
                        !fallback
                        && amount <= 25_000m
                        && (day + item) % 3 != 0;
                    generatedPayments[^1].RawPayload["convertedToInstant"] = false;
                }
            }
        }

        if (generatedPayments.Count > 0)
        {
            context.PaymentEvents.AddRange(generatedPayments);
            context.SaveChanges();
        }

        var paymentIds = context.PaymentEvents
            .Where(payment => payment.Reference != null && payment.Reference.StartsWith("DEMO-"))
            .ToList()
            .Where(payment => payments.Any(seed => seed.Reference == payment.Reference))
            .GroupBy(payment => payment.Reference!)
            .ToDictionary(group => group.Key, group => group.First().Id);

        var obligations = new[]
        {
            new ExpectedObligation
            {
                BusinessCustomerId = usdAccount.BusinessCustomerId,
                Source = ExpectedObligationSource.QuickBooks,
                SourceRef = "DEMO-BILL-2026-001",
                CounterpartyName = "Northstar Components",
                Amount = 12_400m,
                DueDate = now.AddDays(4),
                Status = ExpectedObligationStatus.Open,
                CreatedEventAt = now.AddDays(-3),
                CreatedByExt = "demo-seed"
            },
            new ExpectedObligation
            {
                BusinessCustomerId = usdAccount.BusinessCustomerId,
                Source = ExpectedObligationSource.QuickBooks,
                SourceRef = "DEMO-BILL-2026-002",
                CounterpartyName = "Meridian Payroll",
                Amount = 9_300m,
                DueDate = now.AddDays(1),
                Status = ExpectedObligationStatus.Matched,
                CreatedEventAt = now.AddDays(-5),
                CreatedByExt = "demo-seed",
                MatchedPaymentEventId = paymentIds["DEMO-PAY-OUT-ACH-001"]
            },
            new ExpectedObligation
            {
                BusinessCustomerId = usdAccount.BusinessCustomerId,
                Source = ExpectedObligationSource.QuickBooks,
                SourceRef = "DEMO-BILL-2026-003",
                CounterpartyName = "East Coast Logistics",
                Amount = 42_100m,
                DueDate = now.AddDays(-2),
                Status = ExpectedObligationStatus.Open,
                CreatedEventAt = now.AddDays(-12),
                CreatedByExt = "demo-seed"
            },
            new ExpectedObligation
            {
                BusinessCustomerId = usdAccount.BusinessCustomerId,
                Source = ExpectedObligationSource.QuickBooks,
                SourceRef = "DEMO-BILL-2026-004",
                CounterpartyName = "Coastal Freight",
                Amount = 18_750m,
                DueDate = now.AddDays(-1),
                Status = ExpectedObligationStatus.Matched,
                CreatedEventAt = now.AddDays(-7),
                CreatedByExt = "demo-seed",
                MatchedPaymentEventId = paymentIds["DEMO-PAY-OUT-RTP-001"]
            },
            new ExpectedObligation
            {
                BusinessCustomerId = usdAccount.BusinessCustomerId,
                Source = ExpectedObligationSource.QuickBooks,
                SourceRef = "DEMO-BILL-2026-005",
                CounterpartyName = "Westlake Services",
                Amount = 24_600m,
                DueDate = now.AddDays(-14),
                Status = ExpectedObligationStatus.Paid,
                CreatedEventAt = now.AddDays(-22),
                CreatedByExt = "demo-seed",
                MatchedPaymentEventId = paymentIds["DEMO-PAY-OUT-WIRE-001"]
            },
            new ExpectedObligation
            {
                BusinessCustomerId = usdAccount.BusinessCustomerId,
                Source = ExpectedObligationSource.Simulator,
                SourceRef = "DEMO-RFP-IN-FEDNOW-001",
                CounterpartyName = "Apex Hosting",
                Amount = 3_275m,
                DueDate = now.AddDays(2),
                Status = ExpectedObligationStatus.Open,
                CreatedEventAt = now.AddHours(-5),
                CreatedByExt = "demo-seed"
            },
            new ExpectedObligation
            {
                BusinessCustomerId = usdAccount.BusinessCustomerId,
                Source = ExpectedObligationSource.QuickBooks,
                SourceRef = "DEMO-BILL-2026-006",
                CounterpartyName = "Cloudline Utilities",
                Amount = 4_850m,
                DueDate = now.AddDays(7),
                Status = ExpectedObligationStatus.Paid,
                CreatedEventAt = now.AddDays(-2),
                CreatedByExt = "demo-seed",
                MatchedPaymentEventId = paymentIds["DEMO-PAY-OUT-FEDNOW-001"]
            }
        };

        var existingSourceReferences = context.ExpectedObligations
            .Where(obligation => obligation.SourceRef.StartsWith("DEMO-"))
            .Select(obligation => obligation.SourceRef)
            .ToHashSet();

        context.ExpectedObligations.AddRange(
            obligations.Where(obligation => !existingSourceReferences.Contains(obligation.SourceRef)));
        context.SaveChanges();
    }
}
