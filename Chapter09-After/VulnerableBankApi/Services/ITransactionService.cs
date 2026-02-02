using VulnerableBankApi.Dto;
using VulnerableBankApi.Models;

namespace VulnerableBankApi.Services;
public interface ITransactionService
{
    Task<bool> ProcessTransactionAsync(TransactionDto transactionDto);
    Task<IEnumerable<Transaction>> GetAccountTransactionsAsync(Guid id);
}