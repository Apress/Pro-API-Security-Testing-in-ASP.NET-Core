using System.Security.Claims;
using VulnerableBankApi.Dto;
using VulnerableBankApi.Models;

public interface ISecureAccountRepository
{
    Task<AccountResponseDto?> GetAccountWithPermissionsAsync(
        Guid accountId, 
        string userId, 
        string role);
}