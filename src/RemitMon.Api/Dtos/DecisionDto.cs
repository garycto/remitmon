using RemitMon.Domain.Enums;

namespace RemitMon.Api.Dtos;

public record DecisionDto(
    Guid Id,
    Guid PaymentEventId,
    Guid PolicyId,
    int PolicyVersion,
    DecisionOutcome Outcome,
    List<Dictionary<string, object>> Reasons,
    DateTime EvaluatedAt,
    int LatencyMs,
    string ModeApplied
);

public record PaymentEventDto(
    Guid Id,
    Guid AccountId,
    string Rail,
    string Direction,
    string Initiation,
    decimal Amount,
    string Currency,
    string? CounterpartyRef,
    Guid? CounterpartyId,
    string? Memo,
    string? Reference,
    DateTime EventTime,
    DateTime? SettlementDate
);

public record ExceptionDto(
    Guid Id,
    Guid DecisionId,
    string Status,
    Guid? AssignedToUserId,
    DateTime? ExpiresAt,
    DateTime CreatedAt
);

public record ApproveExceptionRequest(
    string Note
);

public record RejectExceptionRequest(
    string Note,
    string Reason
);
