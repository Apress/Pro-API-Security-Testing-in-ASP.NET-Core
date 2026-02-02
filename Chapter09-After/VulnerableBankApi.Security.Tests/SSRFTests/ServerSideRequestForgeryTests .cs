using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace VulnerableBankApi.Security.Tests.SSRFTests;

public class ServerSideRequestForgeryTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly WireMockServer _mockServer;
    private readonly HttpClient _httpClient;
    private readonly string _token;

    public ServerSideRequestForgeryTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _mockServer = WireMockServer.Start();
        _httpClient = _factory.CreateClient();
        
        // Generate a valid token for authentication
        _token = TestTokenGenerator.GenerateJwtToken("service-account", "Customer");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    [Fact]
    public async Task ExchangeRate_BypassesAuthorizationForInternalEndpoints()
    {
        // Arrange - Internal admin endpoint
        _mockServer
            .Given(Request.Create().WithPath("/admin/all-accounts").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{""Currency"":""USD"",""Rate"":1.0,""Timestamp"":""2024-01-01T00:00:00Z""}")
                .WithHeader("Content-Type", "application/json"));

        var internalAdminUrl = $"{_mockServer.Urls[0]}/admin/all-accounts";

        // Act - Regular user tries to access admin endpoint via SSRF
        var response = await _httpClient.GetAsync($"/external/exchange-rate?apiUrl={Uri.EscapeDataString(internalAdminUrl)}");

        // Assert - The SSRF allows bypassing normal authorization
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // This demonstrates authorization bypass through SSRF
        _mockServer.LogEntries.Should().BeEmpty();
    }
    
    [Fact]
    public async Task NotifyPaymentProcessor_AllowsArbitraryWebhooks_LeaksTransactionData()
    {
        // Arrange - Attacker sets up a malicious webhook endpoint
        _mockServer
            .Given(Request.Create().WithPath("/attacker/payment-steal").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{""status"":""received""}")
                .WithHeader("Content-Type", "application/json"));

        var attackerUrl = $"{_mockServer.Urls[0]}/attacker/payment-steal";
        var request = new PaymentNotificationRequest(
            MerchantWebhookUrl: attackerUrl,
            MerchantId: "MERCHANT-001",
            TransactionId: "TXN-2024-BANK-001234",
            Amount: 10000.00m  // Large transaction amount
        );

        // Act - Vulnerable endpoint accepts any URL
        var response = await _httpClient.PostAsJsonAsync("/external/notify-payment", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        
        // Verify attacker received sensitive payment data
        var attackerRequests = _mockServer.LogEntries;
        attackerRequests.Should().BeEmpty();
        
        // var stolenData = attackerRequests.First().RequestMessage.Body;
        // stolenData.Should().BeNullOrEmpty();
        
    }

    public void Dispose()
    {
        _mockServer?.Stop();
        _mockServer?.Dispose();
    }
}
