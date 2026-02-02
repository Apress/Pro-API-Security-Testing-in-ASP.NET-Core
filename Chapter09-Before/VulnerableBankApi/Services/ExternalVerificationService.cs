using System.Text.Json;
using VulnerableBankApi.Services;

public class ExternalVerificationService : IExternalVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalVerificationService> _logger;

    public ExternalVerificationService(HttpClient httpClient, ILogger<ExternalVerificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // VULNERABILITY: No URL validation - allows SSRF attacks
    public async Task<ExchangeRateResponse?> GetExchangeRateAsync(string apiUrl)
    {
        try
        {
            // DANGEROUS: Directly uses user-provided URL without validation
            _logger.LogInformation("Fetching exchange rate from: {Url}", apiUrl);

            var response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ExchangeRateResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exchange rate from {Url}", apiUrl);
            throw;
        }
    }
    
    // VULNERABILITY: Payment processor notification without validation
    public async Task<bool> NotifyPaymentProcessorAsync(string merchantWebhookUrl, string transactionId, decimal amount)
    {
        try
        {
            // CRITICAL VULNERABILITY: No validation of merchant URL
            // Could be used to:
            // - Send payment data to attacker's server
            // - Access internal payment systems
            // - Perform SSRF attacks on internal services
            
            _logger.LogWarning("Sending payment notification to unvalidated URL: {Url}", merchantWebhookUrl);
            
            // DANGEROUS: Leaking sensitive internal information
            var payload = new PaymentWebhookPayload(
                TransactionId: transactionId,
                Amount: amount,
                Status: "completed",
                Timestamp: DateTime.UtcNow,
                InternalTransactionRef: $"INT-TXN-{Guid.NewGuid()}", // Leaks internal reference format
                ProcessingNode: Environment.MachineName,              // Leaks server name
                DatabaseShard: "shard-03-prod"                       // Leaks database architecture
            );
            
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            
            // No timeout, no validation, no security headers
            var response = await _httpClient.PostAsync(merchantWebhookUrl, content);
            
            // Returns success even if the URL was malicious
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying payment processor at {Url}", merchantWebhookUrl);
            return false;
        }
    }

    // VULNERABILITY: Allows access to internal endpoints
    public async Task<BankVerificationResponse?> VerifyExternalBankAsync(string bankUrl, string routingNumber)
    {
        try
        {
            // DANGEROUS: No validation of bankUrl - could be internal service
            var verificationUrl = $"{bankUrl}/api/verify/{routingNumber}";
            _logger.LogInformation("Verifying bank at: {Url}", verificationUrl);
            
            var response = await _httpClient.GetAsync(verificationUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<BankVerificationResponse>(content, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying bank at {Url}", bankUrl);
            return null;
        }
    }

    // VULNERABILITY: Generic data fetcher - extremely dangerous
    public async Task<string> FetchExternalDataAsync(string url)
    {
        try
        {
            // CRITICAL VULNERABILITY: Fetches any URL without validation
            // Could access:
            // - Internal services (http://localhost:5000/admin)
            // - Cloud metadata endpoints (http://169.254.169.254/latest/meta-data/)
            // - Internal network resources (http://192.168.1.1:8080/config)
            // - File URLs (file:///etc/passwd on some systems)
            
            _logger.LogWarning("Fetching data from arbitrary URL: {Url}", url);
            
            var response = await _httpClient.GetAsync(url);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data from {Url}", url);
            throw;
        }
    }

    // VULNERABILITY: Webhook callback without validation
    public async Task<AccountVerificationResponse?> VerifyAccountWithWebhookAsync(string webhookUrl, string accountNumber)
    {
        try
        {
            // DANGEROUS: Posts sensitive data to user-controlled URL
            var payload = new
            {
                accountNumber = accountNumber,
                timestamp = DateTime.UtcNow,
                bankCode = "VBANK001",
                // Leaking internal information
                internalIp = "192.168.1.100",
                databaseServer = "db-prod-01.internal"
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            
            _logger.LogInformation("Sending verification to webhook: {Url}", webhookUrl);
            var response = await _httpClient.PostAsync(webhookUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AccountVerificationResponse>(responseContent);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling webhook at {Url}", webhookUrl);
            return null;
        }
    }
}