namespace VulnerableBankApi.Dto;
public record TransactionDto(Guid FromAccountId, Guid ToAccountId, decimal Amount);
public record TransactionResponseDto(Guid FromAccountId, Guid ToAccountId, decimal Amount);
