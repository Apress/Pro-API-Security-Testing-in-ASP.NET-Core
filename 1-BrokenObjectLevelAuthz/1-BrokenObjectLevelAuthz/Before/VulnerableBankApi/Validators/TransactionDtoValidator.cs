using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VulnerableBankApi.Data;
using VulnerableBankApi.Dto;

namespace VulnerableBankApi.Validators;
public class TransactionDtoValidator : AbstractValidator<TransactionDto>
{
    private readonly BankDbContext _context;

    public TransactionDtoValidator(BankDbContext context)
    {
        _context = context;

        RuleFor(x => x.FromAccountId)
            .NotEmpty()
            .WithMessage("Source account ID is required.")
            .Must(BeValidGuid)
            .WithMessage("Invalid source account ID format.")
            .MustAsync(AccountExistsAsync)
            .WithMessage("Source account does not exist.");

        RuleFor(x => x.ToAccountId)
            .NotEmpty()
            .WithMessage("Destination account ID is required.")
            .Must(BeValidGuid)
            .WithMessage("Invalid destination account ID format.")
            .MustAsync(AccountExistsAsync)
            .WithMessage("Destination account does not exist.")
            .Must((dto, toId) => toId != dto.FromAccountId)
            .WithMessage("Source and destination accounts cannot be the same.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0.")
            .LessThan(1000000)
            .WithMessage("Transaction amount cannot exceed 1,000,000.")
            .PrecisionScale(18, 2, true)
            .WithMessage("Amount cannot have more than 2 decimal places.");

        RuleFor(x => x)
            .MustAsync(HaveSufficientFundsAsync)
            .WithMessage("Insufficient funds in source account.")
            .MustAsync(NotExceedDailyTransactionLimitAsync)
            .WithMessage("Daily transaction limit exceeded for source account.");
    }

    private bool BeValidGuid(Guid id)
    {
        return id != Guid.Empty;
    }

    private async Task<bool> AccountExistsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        return await _context.Accounts.AnyAsync(a => a.Id == accountId, cancellationToken);
    }

    private async Task<bool> HaveSufficientFundsAsync(TransactionDto dto, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == dto.FromAccountId, cancellationToken);
        
        return account?.Balance >= dto.Amount;
    }

    private async Task<bool> NotExceedDailyTransactionLimitAsync(TransactionDto dto, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var dailyTransactionsTotal = await _context.Transactions
            .Where(t => t.FromAccountId == dto.FromAccountId)
            .Where(t => t.Timestamp >= today && t.Timestamp < tomorrow)
            .SumAsync(t => t.Amount, cancellationToken);

        // Assuming a daily transaction limit of 10,000
        const decimal dailyLimit = 10000m;
        return (dailyTransactionsTotal + dto.Amount) <= dailyLimit;
    }
}