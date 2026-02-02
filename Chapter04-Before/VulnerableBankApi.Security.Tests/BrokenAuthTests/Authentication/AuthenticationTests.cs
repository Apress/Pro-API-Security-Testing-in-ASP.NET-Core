using System.Net;
using FluentAssertions;
using System.Net.Http.Headers;
using System.Text;

namespace VulnerableBankApi.Security.Tests;

public class AuthenticationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;
    // private readonly string _validIssuer = "https://example.com";
    // private readonly string _validAudience = "https://example.com";

    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task GetTransactions_WithoutAuthentication_ShouldReturnUnauthorized(string targetAccountId)
    {
        // Arrange
        var _httpClient = _factory.CreateClient();

        // Act
        var response = await _httpClient.GetAsync($"accounts/{targetAccountId}/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task ValidateToken_WithManipulatedHeader_ShouldRejectTokenAsync(string targetAccountId)
    {
        var requestingUserId = "52d43d2f-8859-4c38-9357-e1f41e21b3f8"; 
        var requestingUserIdAccounts = new List<string> { "7ca61183-46cf-4e8d-b506-9b787e45f2d9" };
        var token = TestTokenGenerator.GenerateJwtToken(requestingUserId, requestingUserIdAccounts);
        
        // Manipulate the token header
        var manipulatedToken = TamperWithToken(token);

        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", manipulatedToken);

        // Act
        var response = await _httpClient.GetAsync($"accounts/{targetAccountId}/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    }

    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task GenerateToken_WithPastExpiration_ShouldThrowExceptionAsync(string targetAccountId)
    {
        // Arrange
        var requestingUserId = "52d43d2f-8859-4c38-9357-e1f41e21b3f8"; 
        var requestingUserIdAccounts = new List<string> { "7ca61183-46cf-4e8d-b506-9b787e45f2d9" };
        var token = TestTokenGenerator.GenerateTokenWithCustomExpiration(requestingUserId, TimeSpan.FromMinutes(-5), requestingUserIdAccounts);

        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _httpClient.GetAsync($"accounts/{targetAccountId}/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    }    

    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task GenerateToken_WithNoExpiration_ShouldThrowExceptionAsync(string targetAccountId)
    {
        // Arrange
        var requestingUserId = "52d43d2f-8859-4c38-9357-e1f41e21b3f8"; 
        var requestingUserIdAccounts = new List<string> { "7ca61183-46cf-4e8d-b506-9b787e45f2d9" };
        var token = TestTokenGenerator.GenerateTokenWithNoExpiration(requestingUserId, requestingUserIdAccounts);
        
        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _httpClient.GetAsync($"accounts/{targetAccountId}/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    }    

    private static string TamperWithToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) throw new ArgumentException("Invalid JWT token");

        // Manipulate the header to use "none" algorithm
        var headerJson = "{\"alg\":\"none\",\"typ\":\"JWT\"}";
        var headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return $"{headerBase64}.{parts[1]}.{parts[2]}";
    }    
    // public async Task GetAccounts_WithExpiredToken_NoClockSkew_ShouldReturnUnauthorized()
    //     {
    //         // Arrange
    //         var expiredToken = TestTokenGenerator.GenerateToken(
    //             issuer: _validIssuer,
    //             audience: _validAudience,
    //             expires: DateTime.UtcNow.AddSeconds(-1) // Expired 1 second ago
    //         );

    //         var _httpClient = _factory.CreateClient();

    //         _httpClient.DefaultRequestHeaders.Authorization = 
    //             new AuthenticationHeaderValue("Bearer", expiredToken);

    //         // Act
    //         var response = await _httpClient.GetAsync("/api/accounts");

    //         // Assert
    //         response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    //         var error = await response.Content.ReadAsStringAsync();
    //         error.Should().Contain("expired");
    //     }

}

