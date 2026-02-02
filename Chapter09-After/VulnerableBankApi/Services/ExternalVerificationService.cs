using System.Net;
using System.Text.Json;
using VulnerableBankApi.Services;

public class ExternalVerificationService : IExternalVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalVerificationService> _logger;
    private readonly HashSet<string> _allowedDomains
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "api.exchangerate-api.com",
                "api.currencyapi.com",
                "api.fixer.io",
                "api.exchangeratesapi.io"
            };    
    public List<string> AllowedProtocols { get; set; } = new() { "https" };
 
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

            // Validate URL
            if (!IsUrlSafe(apiUrl, requireAllowedDomain: true))
            {
                _logger.LogWarning("Blocked unsafe URL attempt: {Url}", apiUrl);
                throw new SecurityException("Invalid or unsafe URL provided");
            }

            // Additional validation for exchange rate endpoints
            if (!IsExchangeRateEndpoint(apiUrl))
            {
                _logger.LogWarning("URL is not a recognized exchange rate API: {Url}", apiUrl);
                throw new SecurityException("URL is not a recognized exchange rate service");
            }

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
            return null;
        }
    }

    public async Task<bool> NotifyPaymentProcessorAsync(string merchantId, string merchantWebhookUrl, string transactionId, decimal amount)
    {
        try
        {

            // In production, this would retrieve from database
            var registeredWebhook = GetRegisteredMerchantWebhook(merchantId);

            if (registeredWebhook == null)
            {
                _logger.LogWarning("No registered webhook for merchant: {MerchantId}", merchantId);
                throw new SecurityException("Merchant webhook not registered");
            }

            // Validate webhook URL is still in approved list
            if (!IsUrlSafe(registeredWebhook.Url, requireAllowedDomain: true))
            {
                _logger.LogWarning("Merchant webhook URL no longer valid: {Url}", registeredWebhook.Url);
                throw new SecurityException("Webhook URL validation failed");
            }

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

            var response = await _httpClient.PostAsync(merchantWebhookUrl, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying payment processor at {Url}", merchantWebhookUrl);
            return false;
        }
    }

    // Helper method to simulate merchant webhook registry
    private MerchantWebhook? GetRegisteredMerchantWebhook(string merchantId)
    {
        // In production, this would query from database
        // For demo, using a pre-configured list of approved merchants
        var approvedMerchants = new Dictionary<string, MerchantWebhook>
        {
            ["MERCHANT-001"] = new MerchantWebhook
            {
                MerchantId = "MERCHANT-001",
                Url = "https://api.stripe.com/v1/webhooks/payment",
                Secret = "whsec_test_secret_key_12345",
                IsActive = true
            },
            ["MERCHANT-002"] = new MerchantWebhook
            {
                MerchantId = "MERCHANT-002",
                Url = "https://api.paypal.com/v1/notifications/webhooks",
                Secret = "whsec_paypal_secret_67890",
                IsActive = true
            }
        };

        return approvedMerchants.TryGetValue(merchantId, out var webhook) ? webhook : null;
    }

    // Security Validation Methods
    
    private bool IsUrlSafe(string url, bool requireAllowedDomain = false)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            // Check protocol
            if (!this.AllowedProtocols.Contains(uri.Scheme.ToLowerInvariant()))
            {
                _logger.LogWarning("Blocked URL with disallowed protocol: {Scheme}", uri.Scheme);
                return false;
            }

            // Block local addresses
            if (IsLocalOrPrivateAddress(uri))
            {
                _logger.LogWarning("Blocked local/private address: {Host}", uri.Host);
                return false;
            }

            // Check against allowed domains if required
            if (requireAllowedDomain && !_allowedDomains.Contains(uri.Host))
            {
                _logger.LogWarning("Host not in allowed list: {Host}", uri.Host);
                return false;
            }

            // Block URLs with credentials
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                _logger.LogWarning("Blocked URL with embedded credentials");
                return false;
            }

            // Block non-standard ports for HTTP/HTTPS
            if ((uri.Scheme == "http" && uri.Port != 80 && uri.Port != -1) ||
                (uri.Scheme == "https" && uri.Port != 443 && uri.Port != -1))
            {
                _logger.LogWarning("Blocked URL with non-standard port: {Port}", uri.Port);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating URL: {Url}", url);
            return false;
        }
    }

    private bool IsExchangeRateEndpoint(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        // Check if the host is in our allowed exchange rate services
        return _allowedDomains.Contains(uri.Host);
    }
    private bool IsLocalOrPrivateAddress(Uri uri)
    {
        try
        {
            // Check for localhost variants
            var lowercaseHost = uri.Host.ToLowerInvariant();
            if (lowercaseHost == "localhost" ||
                lowercaseHost == "127.0.0.1" ||
                lowercaseHost == "::1" ||
                lowercaseHost.EndsWith(".local") ||
                lowercaseHost.EndsWith(".internal"))
            {
                return true;
            }

            // Resolve DNS to check IP
            var addresses = Dns.GetHostAddresses(uri.Host);
            foreach (var address in addresses)
            {
                if (IsPrivateIPAddress(address))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            // If DNS resolution fails, treat as unsafe
            return true;
        }
    }

    private bool IsPrivateIPAddress(IPAddress address)
    {
        // Check for private IP ranges
        byte[] bytes = address.GetAddressBytes();

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            // IPv4 private ranges
            return (bytes[0] == 10) ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 127) || // Loopback
                   (bytes[0] == 169 && bytes[1] == 254); // Link-local
        }
        else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // IPv6 private ranges
            return address.IsIPv6LinkLocal ||
                   address.IsIPv6SiteLocal ||
                   IPAddress.IsLoopback(address);
        }

        return false;
    }
    
}

public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
}

// Helper class for merchant webhook configuration
public class MerchantWebhook
{
    public string MerchantId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
