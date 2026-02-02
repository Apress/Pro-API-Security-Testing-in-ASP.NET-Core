using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace VulnerableBankApi.Security.Tests.SecurityMisconfigurationTests;

public class SecurityMisconfigurationTests(CustomWebApplicationFactory<Program> factory) :
    IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task CORS_Should_Not_Allow_Any_Origin()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/accounts/7ca61183-46cf-4e8d-b506-9b787e45f2d9");
        request.Headers.Add("Origin", "https://localhost:5000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        // Act
        var response = await client.SendAsync(request);

        // Assert - Check if CORS is too permissive
        if (response.Headers.Contains("Access-Control-Allow-Origin"))
        {
            var allowOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
            allowOrigin.Should().NotBe("*",
                "Security misconfiguration: CORS allows any origin");
            allowOrigin.Should().NotBe("http://romancanlas.com",
                "Security misconfiguration: CORS accepts untrusted origins");
        }
    }

    [Fact]
    public async Task API_Should_Not_Allow_Unnecessary_HTTP_Methods()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Try TRACE method (should be disabled)
        var traceRequest = new HttpRequestMessage(HttpMethod.Trace, "/accounts/7ca61183-46cf-4e8d-b506-9b787e45f2d9");
        var traceResponse = await client.SendAsync(traceRequest);

        // Assert
        traceResponse.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed,
            "Security misconfiguration: TRACE method should be disabled");

        // Try HEAD method on sensitive endpoints
        var headRequest = new HttpRequestMessage(HttpMethod.Head, "/accounts/7ca61183-46cf-4e8d-b506-9b787e45f2d9");
        var headResponse = await client.SendAsync(headRequest);

        // HEAD might be acceptable, but should be intentional
        if (headResponse.IsSuccessStatusCode)
        {
            headResponse.Headers.Should().NotContainKey("X-Account-Balance",
                "Security misconfiguration: HEAD method exposes sensitive headers");
        }
    }

    [Fact]
    public async Task API_Should_Not_Expose_Detailed_Errors()
    {
        // Arrange
        var requestingUserId = "52d43d2f-8859-4c38-9357-e1f41e21b3f8"; 
        var requestingUserIdAccounts = new List<string> { "7ca61183-46cf-4e8d-b506-9b787e45f2d9" };
        var token = TestTokenGenerator.GenerateJwtToken(requestingUserId, requestingUserIdAccounts);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Act - Trigger an error by sending invalid data
        var invalidData = new 
        { 
            InterestRate = 10.0m  // This will cause an arithmetic overflow exception
        };

        var response = await client.PutAsJsonAsync("/accounts/7ca61183-46cf-4e8d-b506-9b787e45f2d9", invalidData);
        
        // Assert
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            
            // Check for stack traces or sensitive information
            content.Should().NotContain("StackTrace",
                "Security misconfiguration: Error responses contain stack traces");
            content.Should().NotContain("Microsoft.EntityFrameworkCore",
                "Security misconfiguration: Error responses expose internal frameworks");
            content.Should().NotContain("at VulnerableBankApi",
                "Security misconfiguration: Error responses expose internal namespaces");
        }
    }        

}