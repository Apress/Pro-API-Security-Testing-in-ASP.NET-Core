namespace VulnerableBankApi.Services;

public interface IThirdPartyIntegrationService
{
    Task<string> GetCreditScoreAsync(string taxId);
    Task<object?> ProcessGraphQLQueryAsync(string query);
}