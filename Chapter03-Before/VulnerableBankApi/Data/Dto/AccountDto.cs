using VulnerableBankApi.Models;

namespace VulnerableBankApi.Dto;
public record AccountDto(Guid UserId, decimal InitialBalance, AccountType Type);
