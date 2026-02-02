using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VulnerableBankApi.Data;
using VulnerableBankApi.Dto;
using VulnerableBankApi.Models;

public class SecureAccountRepository  : ISecureAccountRepository
{
    private readonly BankDbContext _context;
    private readonly ILogger<SecureAccountRepository> _logger;
    
    public SecureAccountRepository(
        BankDbContext context,
        ILogger<SecureAccountRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<AccountResponseDto?> GetAccountWithPermissionsAsync(
        Guid accountId, 
        string userId, 
        string role)
    {
        // First, check if the account exists and get the owner
        var accountInfo = await _context.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => new { a.Id, OwnerId = a.UserId.ToString() })
            .FirstOrDefaultAsync();

        if (accountInfo == null)
        {
            return null;
        }
        
        // Determine access level based on role and ownership
        //var accessLevel = DetermineAccessLevel(userId, accountInfo.OwnerId, role);
        var accessLevel = DetermineAccessLevel(userId, role);
        
        _logger.LogInformation(
            "User {UserId} with role {Role} requesting account {AccountId} with access level {AccessLevel}",
            userId, role, accountId, accessLevel);
        
        // Project only the fields the user is authorized to see
        return accessLevel switch
        {
            AccessLevel.None => null,
            AccessLevel.Basic => await GetBasicAccountInfo(accountId),
            AccessLevel.Service => await GetServiceAccountInfo(accountId),
            AccessLevel.Financial => await GetFinancialAccountInfo(accountId),
            AccessLevel.Full => await GetFullAccountInfo(accountId),
            _ => null
        };
    }
    
    private AccessLevel DetermineAccessLevel(string userId, string accountOwnerId, string role)
    {
        // Owner or Admin gets full access
        if (userId == accountOwnerId || role == "Admin")
        {
            return AccessLevel.Full;
        }
        
        // Role-based access
        return role switch
        {
            "Customer" => AccessLevel.Full,
            "FinancialAdvisor" => AccessLevel.Financial,
            "CustomerRepresentative" => AccessLevel.Service,
            "Auditor" => AccessLevel.Basic,
            _ => AccessLevel.None
        };
    }

    private AccessLevel DetermineAccessLevel(string userId, string role)
    {
        // Owner or Admin gets full access
        if (role == "Admin")
        {
            return AccessLevel.Full;
        }
        
        // Role-based access
        return role switch
        {
            "Customer" => AccessLevel.Full,
            "FinancialAdvisor" => AccessLevel.Financial,
            "CustomerRepresentative" => AccessLevel.Service,
            "Auditor" => AccessLevel.Basic,
            "Teller" => AccessLevel.Basic,
            "LoanOfficer" => AccessLevel.Financial,
            _ => AccessLevel.None
        };
    }

    private async Task<AccountResponseDto?> GetBasicAccountInfo(Guid accountId)
    {
        return await _context.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => new AccountResponseDto(
                a.Id,
                a.AccountNumber,
                null, // No balance info
                null,
                null,
                null,
                null, // No routing number
                null, // No TIN
                a.Type,
                a.CreatedAt,
                a.LastModified,
                a.Status
            ))
            .FirstOrDefaultAsync();
    }
    
    private async Task<AccountResponseDto?> GetServiceAccountInfo(Guid accountId)
    {
        return await _context.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => new AccountResponseDto(
                a.Id,
                a.AccountNumber,
                null, // No balance info for customer service
                null,
                null,
                null,
                a.RoutingNumber, // Can see routing number
                null, // No TIN
                a.Type,
                a.CreatedAt,
                a.LastModified,
                a.Status
            ))
            .FirstOrDefaultAsync();
    }
    
    private async Task<AccountResponseDto?> GetFinancialAccountInfo(Guid accountId)
    {
        return await _context.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => new AccountResponseDto(
                a.Id,
                a.AccountNumber,
                a.AvailableBalance, // Can see financial details
                a.CurrentBalance,
                a.CreditLimit,
                a.InterestRate,
                a.RoutingNumber,
                null, // Still no TIN for financial advisors
                a.Type,
                a.CreatedAt,
                a.LastModified,
                a.Status
            ))
            .FirstOrDefaultAsync();
    }
    
    private async Task<AccountResponseDto?> GetFullAccountInfo(Guid accountId)
    {
        return await _context.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => new AccountResponseDto(
                a.Id,
                a.AccountNumber,
                a.AvailableBalance,
                a.CurrentBalance,
                a.CreditLimit,
                a.InterestRate,
                a.RoutingNumber,
                a.TaxIdentificationNumber, // Full access includes TIN
                a.Type,
                a.CreatedAt,
                a.LastModified,
                a.Status
            ))
            .FirstOrDefaultAsync();
    }
    
    private enum AccessLevel
    {
        None,
        Basic,
        Service,
        Financial,
        Full
    }
}