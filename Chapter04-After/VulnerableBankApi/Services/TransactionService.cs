using VulnerableBankApi.Models;
using VulnerableBankApi.Data;
using VulnerableBankApi.Dto;
using Microsoft.EntityFrameworkCore;

namespace VulnerableBankApi.Services;
public class TransactionService : ITransactionService
{
    private readonly BankDbContext _context;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(BankDbContext context, ILogger<TransactionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ProcessTransactionAsync(TransactionDto transactionDto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var fromAccount = await _context.Accounts.FindAsync(transactionDto.FromAccountId);
            var toAccount = await _context.Accounts.FindAsync(transactionDto.ToAccountId);

            if (fromAccount == null || toAccount == null)
            {
                return false;
            }

            if (fromAccount.Balance < transactionDto.Amount)
            {
                return false;
            }

            fromAccount.Balance -= transactionDto.Amount;
            toAccount.Balance += transactionDto.Amount;

            var transactionRecord = new Transaction
            {
                Id = Guid.NewGuid(),
                FromAccountId = transactionDto.FromAccountId,
                ToAccountId = transactionDto.ToAccountId,
                Amount = transactionDto.Amount,
                Timestamp = DateTime.UtcNow
            };

            _context.Transactions.Add(transactionRecord);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Processed transaction: {TransactionId}", transactionRecord?.Id);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing transaction");
            return false;
        }
    }

    public async Task<IEnumerable<Transaction>> GetAccountTransactionsAsync(Guid accountId)
    {
        return await _context.Transactions
            .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
            .ToListAsync();
    }    
}