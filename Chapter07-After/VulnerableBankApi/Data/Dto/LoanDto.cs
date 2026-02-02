using VulnerableBankApi.Models;

public record LoanUpdateDto(
    decimal? ApprovedAmount,
    decimal? InterestRate,
    int? TermMonths,
    LoanStatus? Status,
    string? ApprovalNotes,
    string? RejectionReason
);

public record LoanResponseDto(
    Guid Id,
    Guid AccountId,
    decimal RequestedAmount,
    decimal? ApprovedAmount,
    decimal? InterestRate,
    int? TermMonths,
    LoanStatus Status,
    string? ApprovalNotes,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    string? ApprovedBy
);