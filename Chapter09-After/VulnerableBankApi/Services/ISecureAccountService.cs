using System.Security.Claims;
using VulnerableBankApi.Dto;

namespace VulnerableBankApi.Services;
public interface ISecureAccountService
{
    Task<AccountResponseDto?> GetAccountAsync(Guid id, ClaimsPrincipal user);
}