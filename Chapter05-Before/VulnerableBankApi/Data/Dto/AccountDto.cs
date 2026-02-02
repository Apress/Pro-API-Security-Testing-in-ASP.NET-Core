using VulnerableBankApi.Models;

namespace VulnerableBankApi.Dto;
public record AccountCreationDto(
    string AccountNumber,
    AccountType AccountType,
    decimal AvailableBalance,  // Vulnerable to mass assignment
    decimal CurrentBalance,  // Vulnerable to mass assignment
    decimal CreditLimit,      // Vulnerable to mass assignment
    decimal InterestRate,     // Vulnerable to mass assignment
    string TaxIdentificationNumber
);

public record AccountUpdateDto(
    decimal? AvailableBalance,  // Vulnerable to mass assignment
    decimal? CurrentBalance,  // Vulnerable to mass assignment
    decimal? CreditLimit,      // Vulnerable to mass assignment
    decimal? InterestRate     // Vulnerable to mass assignment
);

public record AccountResponseDto(
    Guid Id,
    string AccountNumber,
    decimal? AvailableBalance,  // Vulnerable to mass assignment
    decimal? CurrentBalance,  // Vulnerable to mass assignment
    decimal? CreditLimit,      // Vulnerable to mass assignment
    decimal? InterestRate,     // Vulnerable to mass assignment
    string? RoutingNumber,     
    string? TaxIdentificationNumber,  // Sensitive data exposure
    AccountType AccountType,
    DateTime CreatedAt,
    DateTime LastModified
);
