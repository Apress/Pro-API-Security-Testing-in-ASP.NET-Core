using VulnerableBankApi.Models;
using VulnerableBankApi.Data;
using VulnerableBankApi.Dto;

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

    public async Task<Account?> GetAccountAsync(Guid id)
    {
        return await _context.Accounts.FindAsync(id);
    }

    public async Task<Account> CreateAccountAsync(AccountDto accountDto)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = accountDto.UserId,
            AccountNumber = GenerateAccountNumber(),
            Balance = accountDto.InitialBalance,
            Type = accountDto.Type,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new account: {AccountId}", account.Id);

        return account;
    }

    public async Task<bool> UpdateAccountAsync(Guid id, AccountDto accountDto)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account == null) return false;

        account.Balance = accountDto.InitialBalance;
        account.Type = accountDto.Type;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated account: {AccountId}", id);

        return true;
    }

    private static string GenerateAccountNumber()
    {
        return Guid.NewGuid().ToString("N")[..10].ToUpper();
    }
}