using VulnerableBankApi.Dto;

namespace VulnerableBankApi.Services;

public interface ITransactionService
{
    Task<bool> ProcessTransactionAsync(TransactionDto transactionDto);
    Task<IEnumerable<TransactionDto>> GetAccountTransactionsAsync(Guid accountId);
    Task<TransactionResponseDto?> ProcessTransferAsync(TransactionDto transactionDto);
    Task<bool> ProcessBulkTransfersAsync(IEnumerable<TransactionDto> transactions);
    Task<decimal> GetDailyTransferTotalAsync(Guid accountId);    
}