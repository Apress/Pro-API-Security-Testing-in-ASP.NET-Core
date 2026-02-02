namespace VulnerableBankApi.Dto;
public record TransactionDto(Guid FromAccountId, Guid ToAccountId, decimal Amount);
