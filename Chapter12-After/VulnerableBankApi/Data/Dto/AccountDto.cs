using VulnerableBankApi.Models;

namespace VulnerableBankApi.Dto;
//public record AccountDto(Guid UserId, decimal InitialBalance, AccountType Type);
public record AccountCreationDto(
    Guid Id,
    string AccountNumber,
    AccountType AccountType,
    decimal AvailableBalance,  // Vulnerable to mass assignment
    decimal CurrentBalance,  // Vulnerable to mass assignment
    decimal CreditLimit,      // Vulnerable to mass assignment
    decimal InterestRate,     // Vulnerable to mass assignment
    string TaxIdentificationNumber,
    string RoutingNumber,
    AccountType Type,
    DateTime CreatedAt,
    DateTime LastModified
);
public record AccountUpdateDto(
    decimal? AvailableBalance,  // Vulnerable to mass assignment
    decimal? CurrentBalance,  // Vulnerable to mass assignment
    decimal? CreditLimit,      // Vulnerable to mass assignment
    decimal? InterestRate     // Vulnerable to mass assignment
);

// Unified response DTO that maps to the original AccountResponseDto structure
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

// Base information visible to all authenticated users
public record AccountBasicInfoDto(
    Guid Id,
    string AccountNumber,
    AccountType AccountType,
    DateTime CreatedAt,
    DateTime LastModified
);

// Extended information for customer service representatives
public record AccountServiceInfoDto(
    Guid Id,
    string AccountNumber,
    AccountType AccountType,
    string? RoutingNumber,
    DateTime CreatedAt,
    DateTime LastModified
) : AccountBasicInfoDto(Id, AccountNumber, AccountType, CreatedAt, LastModified);

// Financial details for financial advisors
public record AccountFinancialInfoDto(
    Guid Id,
    string AccountNumber,
    AccountType AccountType,
    decimal? AvailableBalance,
    decimal? CurrentBalance,
    decimal? CreditLimit,
    decimal? InterestRate,
    string? RoutingNumber,
    DateTime CreatedAt,
    DateTime LastModified
) : AccountServiceInfoDto(Id, AccountNumber, AccountType, RoutingNumber, CreatedAt, LastModified);

// Complete information for account owners and admins
public record AccountFullInfoDto(
    Guid Id,
    string AccountNumber,
    AccountType AccountType,
    decimal? AvailableBalance,
    decimal? CurrentBalance,
    decimal? CreditLimit,
    decimal? InterestRate,
    string? RoutingNumber,
    string? TaxIdentificationNumber,  // Only in full view
    DateTime CreatedAt,
    DateTime LastModified
) : AccountFinancialInfoDto(Id, AccountNumber, AccountType, AvailableBalance, CurrentBalance, 
    CreditLimit, InterestRate, RoutingNumber, CreatedAt, LastModified);
