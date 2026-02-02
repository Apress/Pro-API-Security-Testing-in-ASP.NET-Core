using Microsoft.EntityFrameworkCore;
using VulnerableBankApi.Data;
using VulnerableBankApi.Dto;
using VulnerableBankApi.Models;

namespace VulnerableBankApi.Services;

public class LoanService : ILoanService
{
    private readonly BankDbContext _context;
    private readonly ILogger<LoanService> _logger;

    public LoanService(BankDbContext context, ILogger<LoanService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LoanResponseDto?> GetLoanAsync(Guid accountId, Guid loanId)
    {
        var loan = await _context.Loans
            .Where(l => l.Id == loanId && l.AccountId == accountId)
            .FirstOrDefaultAsync();

        if (loan == null)
            return null;

        return MapToResponseDto(loan);
    }

    public async Task<LoanResponseDto?> UpdateLoanAsync(Guid accountId, Guid loanId, LoanUpdateDto loanUpdateDto)
    {
        // VULNERABILITY: This method performs loan approval without checking user roles
        // Should verify the user has LoanOfficer role and check approval limits:
        // - Under $10K: LoanOfficer
        // - $10K-$100K: BranchManager  
        // - Over $100K: RegionalManager
        
        var loan = await _context.Loans
            .Where(l => l.Id == loanId && l.AccountId == accountId)
            .FirstOrDefaultAsync();

        if (loan == null)
        {
            _logger.LogWarning("Loan {LoanId} not found for account {AccountId}", loanId, accountId);
            return null;
        }

        // VULNERABILITY: No role or amount-based authorization check
        if (loanUpdateDto.ApprovedAmount.HasValue)
        {
            loan.ApprovedAmount = loanUpdateDto.ApprovedAmount.Value;
            
            // Log the unauthorized approval
            _logger.LogCritical("UNAUTHORIZED LOAN APPROVAL: Loan {LoanId} approved for ${Amount} without authorization check", 
                loanId, loanUpdateDto.ApprovedAmount.Value);
        }

        if (loanUpdateDto.InterestRate.HasValue)
            loan.InterestRate = loanUpdateDto.InterestRate.Value;
            
        if (loanUpdateDto.TermMonths.HasValue)
            loan.TermMonths = loanUpdateDto.TermMonths.Value;
            
        if (loanUpdateDto.Status.HasValue)
        {
            loan.Status = loanUpdateDto.Status.Value;
            
            if (loanUpdateDto.Status.Value == LoanStatus.Approved)
            {
                loan.ApprovedAt = DateTime.UtcNow;
                loan.ApprovedBy = "UNAUTHORIZED_APPROVER"; // Shows the vulnerability
            }
        }
        
        if (!string.IsNullOrEmpty(loanUpdateDto.ApprovalNotes))
            loan.ApprovalNotes = loanUpdateDto.ApprovalNotes;
            
        if (!string.IsNullOrEmpty(loanUpdateDto.RejectionReason))
            loan.RejectionReason = loanUpdateDto.RejectionReason;

        loan.LastModified = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return MapToResponseDto(loan);
    }

    public async Task<IEnumerable<LoanResponseDto>> GetAccountLoansAsync(Guid accountId)
    {
        var loans = await _context.Loans
            .Where(l => l.AccountId == accountId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return loans.Select(MapToResponseDto);
    }

    private static LoanResponseDto MapToResponseDto(Loan loan)
    {
        return new LoanResponseDto(
            Id: loan.Id,
            AccountId: loan.AccountId,
            RequestedAmount: loan.RequestedAmount,
            ApprovedAmount: loan.ApprovedAmount,
            InterestRate: loan.InterestRate,
            TermMonths: loan.TermMonths,
            Status: loan.Status,
            ApprovalNotes: loan.ApprovalNotes,
            RejectionReason: loan.RejectionReason,
            CreatedAt: loan.CreatedAt,
            ApprovedAt: loan.ApprovedAt,
            ApprovedBy: loan.ApprovedBy
        );
    }
}


