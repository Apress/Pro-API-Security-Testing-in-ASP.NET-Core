using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using VulnerableBankApi.Services;

namespace VulnerableBankApi.Security.Tests;

public class UnsafeApiConsumptionTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;
    private readonly MockHttpMessageHandler _mockHandler = new();

    [Theory]
    [InlineData("52d43d2f-8859-4c38-9357-e1f41e21b3f8")]
    public async Task ThirdPartyApi_GraphQLIntrospection_InformationDisclosure(string accountId)
    {
        // Arrange
        var token = TestTokenGenerator.GenerateJwtToken(accountId);
        
        // Create a client with the real GraphQL service that will call the actual endpoint
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Add the secure third-party integration service for GraphQL
                services.RemoveAll<IThirdPartyIntegrationService>();
                services.AddHttpClient<IThirdPartyIntegrationService, ThirdPartyIntegrationService>()
                    .ConfigureHttpClient(httpClient =>
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(30);
                    });
            });
        }).CreateClient();
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var introspectionQuery = @"{ __schema { types { name kind description fields { name type { name kind } } } queryType { name fields { name description args { name type { name kind } } } } mutationType { name fields { name description } } } }";

        // Act - Call the API endpoint that uses GraphQL introspection
        var response = await client.PostAsJsonAsync("/accounts/graphql", introspectionQuery);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Endpoint should return OK");
        
        var responseContent = await response.Content.ReadAsStringAsync();

        // If empty, no schema was exposed — that's the secure behavior we expect
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return;
        }

        var jsonDocument = JsonDocument.Parse(responseContent);
        var root = jsonDocument.RootElement;

        // Ensure no __schema property is exposed
        root.TryGetProperty("data", out var data);
        data.TryGetProperty("__schema", out var schema).Should().BeFalse(
            "API endpoint does not expose complete __schema through third-party GraphQL");
        
        // SECURITY ISSUES:
        // 1. Banking API endpoint exposes third-party GraphQL schema
        // 2. Introspection allows attackers to understand the entire third-party API structure
        // 3. No filtering or sanitization of third-party schema information
        // 4. Violates principle of least privilege - exposes unnecessary information
    }

    [Theory]
    [InlineData("52d43d2f-8859-4c38-9357-e1f41e21b3f8", "7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task ThirdPartyApi_UsesHttp_VulnerableToMitm(string userId, string accountId)
    {
        // Arrange
        var accounts = new List<string> { accountId };        
        var token = TestTokenGenerator.GenerateJwtToken(userId, accounts);
        
        // Create a client with mocked third-party service to intercept HTTP calls
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the third-party service with our mock version
                services.RemoveAll<IThirdPartyIntegrationService>();
                
                // Add a mock service that captures the HTTP calls
                services.AddSingleton(_mockHandler);
                services.AddHttpClient<IThirdPartyIntegrationService, ThirdPartyIntegrationService>(
                    "mock-credit-service",
                    client => { })
                    .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<MockHttpMessageHandler>());
                
                // Override the factory to inject the localhost mock URL
                services.AddScoped<IThirdPartyIntegrationService>(sp =>
                {
                    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                    var logger = sp.GetRequiredService<ILogger<ThirdPartyIntegrationService>>();
                    var httpClient = httpClientFactory.CreateClient("mock-credit-service");
                    return new ThirdPartyIntegrationService(httpClient, logger, "http://localhost:5001/api/creditscore");
                });
            });
        }).CreateClient();
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Call the API endpoint that retrieves credit score using unsafe HTTP
        var response = await client.GetAsync($"/accounts/{accountId}/creditscore");
                
        // Assert - Check the captured third-party HTTP calls
        _mockHandler.CapturedRequests.Should().NotBeEmpty("API should make third-party calls");
        
        var creditRequest = _mockHandler.CapturedRequests.FirstOrDefault(r => 
            r.RequestUri?.ToString().Contains("localhost") == true);
        
        creditRequest.Should().NotBeNull("API endpoint should call credit score service");
        
        if (creditRequest != null)
        {
            // VULNERABILITY CONFIRMED: Banking API makes HTTP calls to third-party services
            creditRequest.RequestUri?.Scheme.Should().NotBe("http", 
                "Banking API integrates with a third-party over unencrypted HTTP");
            
        }

        // CRITICAL SECURITY ISSUES CONFIRMED:
        // 1. Banking API makes unencrypted HTTP calls to third-party services
        // 2. Vulnerable to Man-in-the-Middle attacks at the API level
        // 3. No validation of third-party SSL certificates
    }

    // Helper class to mock HTTP calls and capture requests
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> CapturedRequests { get; } = new List<HttpRequestMessage>();
        public List<string> CapturedRequestBodies { get; } = new List<string>();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            // Capture the request for inspection
            CapturedRequests.Add(request);
            
            // Capture request body if present
            if (request.Content != null)
            {
                var body = await request.Content.ReadAsStringAsync();
                CapturedRequestBodies.Add(body);
            }
            
            // Return appropriate mock responses based on the URL
            if (request.RequestUri?.ToString().Contains("creditscores") == true || 
                request.RequestUri?.ToString().Contains("localhost") == true)
            {
                // Mock Credit Score API response with hardcoded JSON
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { 
                            score = 750, 
                            rating = "Excellent",
                            status = "Active",
                            lastUpdated = DateTime.UtcNow
                        }), 
                        Encoding.UTF8, 
                        "application/json")
                };
            }
            
            // Default response
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { result = "mock" }), 
                    Encoding.UTF8, 
                    "application/json")
            };
        }
    }
}