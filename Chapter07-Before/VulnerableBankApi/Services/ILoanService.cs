
namespace VulnerableBankApi.Services;
public interface ILoanService
{
    Task<LoanResponseDto?> GetLoanAsync(Guid accountId, Guid loanId);
    Task<LoanResponseDto?> UpdateLoanAsync(Guid accountId, Guid loanId, LoanUpdateDto loanUpdateDto);
    Task<IEnumerable<LoanResponseDto>> GetAccountLoansAsync(Guid accountId);
}