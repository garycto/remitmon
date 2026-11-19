using RemitMon.Domain.Entities;

namespace RemitMon.Infrastructure.Adapters;

public sealed record ModernTreasuryDataResult(
    IReadOnlyList<Counterparty> Counterparties,
    IReadOnlyList<PaymentEvent> PaymentOrders,
    IReadOnlyList<PaymentEvent> IncomingPaymentDetails);
