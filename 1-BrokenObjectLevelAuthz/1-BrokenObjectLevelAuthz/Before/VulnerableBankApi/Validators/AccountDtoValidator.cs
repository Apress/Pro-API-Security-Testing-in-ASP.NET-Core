using FluentValidation;
using VulnerableBankApi.Data;
using VulnerableBankApi.Dto;
using VulnerableBankApi.Models;

namespace VulnerableBankApi.Validators;
public class AccountDtoValidator : AbstractValidator<AccountDto>
{
    private readonly BankDbContext _context;

    public AccountDtoValidator(BankDbContext context)
    {
        _context = context;

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.")
            .Must(BeValidGuid)
            .WithMessage("Invalid User ID format.");

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Initial balance must be greater than or equal to 0.")
            .LessThan(1000000)
            .WithMessage("Initial balance cannot exceed 1,000,000.")
            .PrecisionScale(18, 2, true)
            .WithMessage("Balance cannot have more than 2 decimal places.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid account type.")
            .Must(BeValidAccountTypeForBalance)
            .WithMessage("Initial balance does not meet minimum requirements for selected account type.")
            .When(x => x.Type != default && x.InitialBalance >= 0);
    }

    private bool BeValidGuid(Guid id)
    {
        return id != Guid.Empty;
    }

    private bool BeValidAccountTypeForBalance(AccountDto dto, AccountType type)
    {
        return type switch
        {
            AccountType.Checking => dto.InitialBalance >= 0,
            AccountType.Savings => dto.InitialBalance >= 100,  // Minimum balance for savings
            AccountType.Investment => dto.InitialBalance >= 1000,  // Minimum balance for investment
            _ => false
        };
    }
}