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

            if (fromAccount.CurrentBalance < transactionDto.Amount)
            {
                return false;
            }

            fromAccount.CurrentBalance -= transactionDto.Amount;
            toAccount.CurrentBalance += transactionDto.Amount;

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

    public async Task<IEnumerable<TransactionDto>> GetAccountTransactionsAsync(Guid accountId)
    {
        return await _context.Transactions
            .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
            .Select(t => new TransactionDto(
                t.FromAccountId,
                t.ToAccountId,
                t.Amount
            ))
            .ToListAsync();
    }

    public async Task<TransactionResponseDto?> ProcessTransferAsync(TransactionDto transactionDto)
    {
        // VULNERABILITY 1: No velocity checks (unlimited transfers per day)
        // VULNERABILITY 2: No fraud detection or anomaly checks
        // VULNERABILITY 3: No validation of transfer patterns
        // VULNERABILITY 4: No progressive delays or CAPTCHA for multiple transfers
        // VULNERABILITY 5: No notification or confirmation for large transfers

        var fromAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == transactionDto.FromAccountId);
            
        var toAccount = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == transactionDto.ToAccountId);

        if (fromAccount == null || toAccount == null)
        {
            _logger.LogWarning("Invalid account IDs in transfer request");
            return null;
        }

        // Basic balance check but no other business rules
        if (fromAccount.CurrentBalance < transactionDto.Amount)
        {
            _logger.LogWarning("Insufficient funds for transfer");
            return null;
        }

        // VULNERABLE: No daily/monthly transfer limits
        // VULNERABLE: No check for rapid successive transfers
        // VULNERABLE: No validation of transfer amount patterns
        
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            FromAccountId = transactionDto.FromAccountId,
            ToAccountId = transactionDto.ToAccountId,
            Amount = transactionDto.Amount,
            Timestamp = DateTime.UtcNow
        };

        // Update balances
        fromAccount.CurrentBalance -= transactionDto.Amount;
        toAccount.CurrentBalance += transactionDto.Amount;

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Transfer processed: {Amount} from {FromAccount} to {ToAccount}", 
            transactionDto.Amount, transactionDto.FromAccountId, transactionDto.ToAccountId);

        return MapToResponseDto(transaction);
    }

    private TransactionResponseDto MapToResponseDto(Transaction transaction)
    {
        return new TransactionResponseDto(
            FromAccountId: transaction.FromAccountId,
            ToAccountId: transaction.ToAccountId,
            Amount: transaction.Amount
        );
    }

    public async Task<bool> ProcessBulkTransfersAsync(IEnumerable<TransactionDto> transactions)
    {
        // VULNERABILITY: No limit on number of bulk transfers
        // VULNERABILITY: No validation of total amount being transferred
        // VULNERABILITY: No delay between transfers
        // VULNERABILITY: Can be used to drain accounts quickly

        foreach (var transfer in transactions)
        {
            await ProcessTransferAsync(transfer);
        }

        return true;
    }

    public async Task<decimal> GetDailyTransferTotalAsync(Guid accountId)
    {
        var today = DateTime.UtcNow.Date;
        
        return await _context.Transactions
            .Where(t => t.FromAccountId == accountId && t.Timestamp.Date == today)
            .SumAsync(t => t.Amount);
    }

}