using VulnerableBankApi.Models;
using VulnerableBankApi.Data;
using VulnerableBankApi.Dto;
using Microsoft.EntityFrameworkCore;

namespace VulnerableBankApi.Services;
public class AccountService : IAccountService
{
    private readonly BankDbContext _context;
    private readonly ILogger<AccountService> _logger;

    public AccountService(BankDbContext context, ILogger<AccountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AccountResponseDto?> GetAccountAsync(Guid accountId)
    {
        var accountData = await _context.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => new 
            {
                a.Id,
                a.AccountNumber,
                a.AvailableBalance,
                a.CurrentBalance,
                a.CreditLimit,
                a.InterestRate,
                a.TaxIdentificationNumber,
                a.RoutingNumber,
                a.Type,
                a.CreatedAt,
                a.LastModified
            })
            .FirstOrDefaultAsync();

        if (accountData == null) return null;

        // Map anonymous type to DTO
        return new AccountResponseDto(
            Id: accountData.Id,
            AccountNumber: accountData.AccountNumber,
            AvailableBalance: accountData.AvailableBalance,
            CurrentBalance: accountData.CurrentBalance,
            CreditLimit: accountData.CreditLimit,
            InterestRate: accountData.InterestRate,
            RoutingNumber: accountData.RoutingNumber,
            TaxIdentificationNumber: accountData.TaxIdentificationNumber,
            AccountType: accountData.Type,
            CreatedAt: accountData.CreatedAt,
            LastModified: accountData.LastModified
        );
    }


    public async Task<AccountResponseDto> CreateAccountAsync(AccountCreationDto accountCreationDto)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = GenerateAccountNumber(),
            AvailableBalance = accountCreationDto.AvailableBalance,
            CurrentBalance =  accountCreationDto.CurrentBalance,
            Type = accountCreationDto.AccountType,
            CreatedAt = DateTime.UtcNow,
            RoutingNumber = "026009593",
            TaxIdentificationNumber = accountCreationDto.TaxIdentificationNumber,
            CreditLimit = accountCreationDto.CreditLimit,
            InterestRate = accountCreationDto.InterestRate,
            LastModified = DateTime.UtcNow,
            Status = AccountStatus.Active        
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new account: {AccountId}", account.Id);

        return MapToResponseDto(account);
        //return account;
    }

    public async Task<AccountResponseDto?> UpdateAccountAsync(Guid accountId,
        AccountUpdateDto accountUpdateDto)
    {        
        // First, check if account exists without loading all data
        var account = await _context.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => a.Id)
            .FirstOrDefaultAsync();
        
        if (account == default) return null;

        if (accountUpdateDto.InterestRate.HasValue)
        {
            await _context.Accounts
                .Where(a => a.Id == accountId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.InterestRate, accountUpdateDto.InterestRate.Value)
                    .SetProperty(a => a.LastModified, DateTime.UtcNow));

            _logger.LogInformation("Bank teller updated interest rate for account {AccountId}", 
                accountId);
        }
                
        return await GetAccountAsync(accountId);
    }

    private static string GenerateAccountNumber()
    {
        return Guid.NewGuid().ToString("N")[..10].ToUpper();
    }

    private AccountResponseDto MapToResponseDto(Account account)
    {
        return new AccountResponseDto(
            Id: account.Id,
            AccountNumber: account.AccountNumber,
            AvailableBalance: account.AvailableBalance,
            CurrentBalance: account.AvailableBalance,
            AccountType: account.Type,
            CreditLimit: account.CreditLimit,    // Sensitive data exposure
            InterestRate: account.InterestRate,  // Sensitive data exposure
            RoutingNumber: account.RoutingNumber,// Sensitive data exposure
            TaxIdentificationNumber: account.TaxIdentificationNumber,// Sensitive data exposure
            CreatedAt: account.CreatedAt,
            LastModified: account.LastModified
        );
    }

}