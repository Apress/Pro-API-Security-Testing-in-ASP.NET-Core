using VulnerableBankApi.Models;
using VulnerableBankApi.Dto;

namespace VulnerableBankApi.Services;
public interface IAccountService
{
    Task<Account?> GetAccountAsync(Guid id);
    Task<Account> CreateAccountAsync(AccountDto accountDto);
    Task<bool> UpdateAccountAsync(Guid id, AccountDto accountDto);
}