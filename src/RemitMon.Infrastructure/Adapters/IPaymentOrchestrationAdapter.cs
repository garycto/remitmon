using RemitMon.Domain.Entities;

namespace RemitMon.Infrastructure.Adapters;

public interface IPaymentOrchestrationAdapter
{
    Task<IReadOnlyList<Counterparty>> ListCounterpartiesAsync(Guid businessCustomerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentEvent>> ListPaymentOrdersAsync(Guid accountId, Guid? counterpartyId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentEvent>> ListIncomingPaymentDetailsAsync(Guid accountId, Guid? counterpartyId = null, CancellationToken cancellationToken = default);
    Task<PaymentEvent?> GetPaymentOrderAsync(Guid accountId, string paymentOrderId, CancellationToken cancellationToken = default);
}
