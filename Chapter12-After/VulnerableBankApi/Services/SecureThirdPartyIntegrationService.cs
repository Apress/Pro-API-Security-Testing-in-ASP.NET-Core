using System.Text;
using System.Text.Json;

namespace VulnerableBankApi.Services;

public class SecureThirdPartyIntegrationService : IThirdPartyIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SecureThirdPartyIntegrationService> _logger;
    private readonly string _creditScoreApiUrl;

    private const string GraphQLApiUrl = "https://graphql-demo.mead.io/";

    public SecureThirdPartyIntegrationService(HttpClient httpClient, ILogger<SecureThirdPartyIntegrationService> logger, string? creditScoreApiUrl = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _creditScoreApiUrl = creditScoreApiUrl ?? "http://localhost:5001/api/creditscore";
    }

    public async Task<string> GetCreditScoreAsync(string taxId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _creditScoreApiUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { taxId = taxId }),
                    Encoding.UTF8,
                    "application/json")
            };

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            _logger.LogWarning("Failed to get credit score: {StatusCode}", response.StatusCode);
            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credit score");
            return "Error";
        }
    }

    public async Task<object?> ProcessGraphQLQueryAsync(string query)
    {
        try
        {
            if (IsIntrospectionQuery(query))
            {
                _logger.LogWarning("Blocked GraphQL introspection query");
                 return null;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, GraphQLApiUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { query = query }),
                    Encoding.UTF8,
                    "application/json")
            };

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<object>(content);
            }

            _logger.LogWarning("Failed to process GraphQL query: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GraphQL query");
            return null;
        }
    }

    private static bool IsIntrospectionQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;

        var q = query!.ToLowerInvariant();
        if (q.Contains("__schema") || q.Contains("__type") || q.Contains("introspection") || q.Contains("__typename"))
            return true;

        return false;
    }
}
