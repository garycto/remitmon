using RemitMon.Domain.Enums;

namespace RemitMon.Api.Dtos;

public record AccountDto(
    Guid Id,
    Guid BusinessCustomerId,
    Guid BankId,
    string DisplayName,
    string MaskedAccountNumber,
    AccountStatus Status,
    AccountMode Mode,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateAccountRequest(
    Guid BusinessCustomerId,
    Guid BankId,
    string DisplayName,
    string MaskedAccountNumber
);

public record UpdateAccountModeRequest(
    AccountMode Mode
);
