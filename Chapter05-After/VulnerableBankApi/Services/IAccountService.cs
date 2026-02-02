using VulnerableBankApi.Dto;

namespace VulnerableBankApi.Services;
public interface IAccountService
{
    Task<AccountResponseDto?> GetAccountAsync(Guid accountId);
    Task<AccountResponseDto> CreateAccountAsync(AccountCreationDto accountDto);
    Task<AccountResponseDto?> UpdateAccountAsync(Guid accountId, AccountUpdateDto accountDto);
}