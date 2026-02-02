using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace VulnerableBankApi.Security.Tests;

public class ImproperInventoryManagementTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Theory]
    [InlineData("52d43d2f-8859-4c38-9357-e1f41e21b3f8", "7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task Multiple_API_Versions_Should_Not_Be_Active_Simultaneously(string userId, string accountId)
    {
        // VULNERABILITY: Multiple API versions running at the same time
        // This test detects if deprecated API versions are still accessible
        
        var client = _factory.CreateClient();
        var accounts = new List<string> { accountId };
        
        var token = TestTokenGenerator.GenerateJwtToken(userId, accounts, "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var endpoints = new[]
        {
            $"/v1/accounts/{accountId}",  // Deprecated version
            $"/v2/accounts/{accountId}",  // Current version
            $"/v3-beta/accounts/{accountId}"  // Beta version
        };

        var debugResponses = new List<string>();
        var successfulResponses = new List<string>();

        foreach (var endpoint in endpoints)
        {
            var response = await client.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();

            // Keep a debug list with all responses for troubleshooting
            debugResponses.Add($"{endpoint} ({response.StatusCode})" +
                (!response.IsSuccessStatusCode ? $" - {content}" : ""));

            // Consider an API version "active" only if it returns a successful status code
            if (response.IsSuccessStatusCode)
            {
                successfulResponses.Add($"{endpoint} ({response.StatusCode})");
            }
        }

        // Assert - Only one API version should be active (successful)
        successfulResponses.Count.Should().BeLessThanOrEqualTo(1,
            "Multiple API versions are active simultaneously. Active responses: " + string.Join(", ", successfulResponses) +
            ". Full responses: " + string.Join(", ", debugResponses));
    }


    [Fact]
    public async Task Swagger_Documentation_Should_Be_Protected_In_Production()
    {
        // VULNERABILITY: Swagger UI exposed in production
        
        var client = _factory.CreateClient();
        
        var swaggerEndpoints = new[]
        {
            "/swagger",
            "/swagger/index.html",
            "/swagger/v1/swagger.json",
            "/swagger/v2/swagger.json",
            "/swagger/v3-beta/swagger.json",
            "/swagger/debug/swagger.json"
        };

        var exposedSwaggerEndpoints = new List<string>();

        foreach (var endpoint in swaggerEndpoints)
        {
            var response = await client.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.MovedPermanently)
            {
                exposedSwaggerEndpoints.Add(endpoint);
            }
        }

        // In production, Swagger should be disabled or protected
        exposedSwaggerEndpoints.Should().BeEmpty(
            "Swagger documentation should not be publicly accessible in production: " + 
            string.Join(", ", exposedSwaggerEndpoints));
    }

}