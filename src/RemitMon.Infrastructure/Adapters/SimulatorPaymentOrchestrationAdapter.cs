using RemitMon.Domain.Entities;
using RemitMon.Domain.Enums;

namespace RemitMon.Infrastructure.Adapters;

public sealed class SimulatorPaymentOrchestrationAdapter : IPaymentOrchestrationAdapter
{
    public Task<IReadOnlyList<Counterparty>> ListCounterpartiesAsync(Guid businessCustomerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Counterparty> result =
        [
            new Counterparty
            {
                BusinessCustomerId = businessCustomerId,
                Name = "Modern Treasury Sandbox - Successful",
                Status = CounterpartyStatus.Approved,
                Source = CounterpartySource.Observed
            },
            new Counterparty
            {
                BusinessCustomerId = businessCustomerId,
                Name = "Modern Treasury Sandbox - Return",
                Status = CounterpartyStatus.Pending,
                Source = CounterpartySource.Observed
            }
        ];
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<PaymentEvent>> ListPaymentOrdersAsync(Guid accountId, Guid? counterpartyId = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PaymentEvent>>(CreateDemoEvents(accountId, counterpartyId));

    public Task<IReadOnlyList<PaymentEvent>> ListIncomingPaymentDetailsAsync(Guid accountId, Guid? counterpartyId = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PaymentEvent>>(CreateDemoEvents(accountId, counterpartyId, incoming: true));

    public Task<PaymentEvent?> GetPaymentOrderAsync(Guid accountId, string paymentOrderId, CancellationToken cancellationToken = default)
    {
        var eventRecord = CreateDemoEvents(accountId, incoming: false).FirstOrDefault();
        if (eventRecord is not null)
            eventRecord.ExternalEventId = paymentOrderId;
        return Task.FromResult(eventRecord);
    }

    private static IReadOnlyList<PaymentEvent> CreateDemoEvents(Guid accountId, Guid? counterpartyId = null, bool incoming = false)
    {
        var now = DateTime.UtcNow;
        return
        [
            new PaymentEvent
            {
                AccountId = accountId,
                ExternalEventId = incoming ? "mt-sandbox-incoming-1" : "mt-sandbox-order-1",
                Rail = Rail.ACH,
                Direction = incoming ? PaymentDirection.Inbound : PaymentDirection.Outbound,
                Initiation = incoming ? PaymentInitiation.Pull : PaymentInitiation.Push,
                Amount = 1250.25m,
                Currency = "USD",
                CounterpartyRef = "Sandbox Successful",
                CounterpartyId = counterpartyId,
                Reference = "sandbox-001",
                Memo = "Simulator data",
                EventTime = now,
                RawPayload = new Dictionary<string, object> { ["source"] = "ModernTreasurySimulator" }
            },
            new PaymentEvent
            {
                AccountId = accountId,
                ExternalEventId = incoming ? "mt-sandbox-incoming-2" : "mt-sandbox-order-2",
                Rail = Rail.RTP,
                Direction = incoming ? PaymentDirection.Inbound : PaymentDirection.Outbound,
                Initiation = incoming ? PaymentInitiation.RfP : PaymentInitiation.Push,
                Amount = 4321.00m,
                Currency = "USD",
                CounterpartyRef = "Sandbox RTP",
                CounterpartyId = counterpartyId,
                Reference = "sandbox-002",
                Memo = "Simulator data",
                EventTime = now.AddMinutes(-30),
                RawPayload = new Dictionary<string, object> { ["source"] = "ModernTreasurySimulator" }
            }
        ];
    }
}
