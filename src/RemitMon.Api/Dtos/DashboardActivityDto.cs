namespace RemitMon.Api.Dtos;

public record DashboardExpectedPaymentDto(
    Guid Id,
    string Source,
    string SourceRef,
    string CounterpartyName,
    decimal Amount,
    DateTime DueDate,
    string Status,
    Guid? MatchedPaymentEventId
);

public record DashboardPaymentDto(
    Guid Id,
    Guid AccountId,
    string AccountName,
    string Rail,
    string Direction,
    string Initiation,
    decimal Amount,
    string Currency,
    string? CounterpartyRef,
    string? Memo,
    string? Reference,
    string? InvoiceRef,
    DateTime EventTime,
    DateTime? SettlementDate,
    string MatchStatus,
    Guid? MatchedObligationId,
    string? MatchedObligationRef,
    string Outcome,
    Guid? PolicyId,
    string PolicyName,
    IReadOnlyList<string> RuleHits,
    bool AchInstantEligible,
    bool ConvertedToInstant
);

public record AchInstantOpportunityDto(
    int Eligible,
    int Converted,
    int Remaining
);

public record DashboardActivityDto(
    IReadOnlyList<DashboardExpectedPaymentDto> ExpectedPayments,
    IReadOnlyList<DashboardPaymentDto> Payments,
    AchInstantOpportunityDto AchInstantOpportunity
);
