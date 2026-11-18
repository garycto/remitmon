using RemitMon.Domain.Enums;

namespace RemitMon.Api.Dtos;

public record CounterpartyDto(
    Guid Id,
    Guid BusinessCustomerId,
    string Name,
    CounterpartyStatus Status,
    CounterpartySource Source,
    List<string> AchCompanyIds,
    List<string> RoutingAccountFingerprints,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateCounterpartyRequest(
    Guid BusinessCustomerId,
    string Name,
    List<string>? AchCompanyIds = null,
    List<string>? RoutingAccountFingerprints = null
);

public record ApproveCounterpartyRequest(
    string Note
);

public record BlockCounterpartyRequest(
    string Reason
);
