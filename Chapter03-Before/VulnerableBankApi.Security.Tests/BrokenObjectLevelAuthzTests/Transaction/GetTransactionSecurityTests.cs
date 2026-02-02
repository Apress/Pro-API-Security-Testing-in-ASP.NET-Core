using System.Net;
using FluentAssertions;
using System.Net.Http.Headers;

namespace VulnerableBankApi.Security.Tests;

public class GetTransactionSecurityTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    // [Fact]
    // public async Task Get_OtherCustomersTransaction_ReturnsAccessDenied()
    // {
    //     // Arrange
    //     var userId1Token = TestTokenGenerator.GenerateJwtToken("52d43d2f-8859-4c38-9357-e1f41e21b3f8");
    //     var userId2Token = TestTokenGenerator.GenerateJwtToken("7ca61183-46cf-4e8d-b506-9b787e45f2d9");

    //     var _httpClient = _factory.CreateClient();
    //     _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userId2Token);

    //     // Act
    //     var response = await _httpClient.GetAsync($"accounts/7ca61183-46cf-4e8d-b506-9b787e45f2d9/transactions");

    //     // Assert
    //     response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

    // }
    
    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task Get_OtherCustomersTransaction_ReturnsAccessDenied(string targetAccountId)
    {
        // Arrange
        // fixed user
        var requestingUserId = "52d43d2f-8859-4c38-9357-e1f41e21b3f8"; 
        var token = TestTokenGenerator.GenerateJwtToken(requestingUserId);
        
        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        // try to access account of another user
        var response = await _httpClient.GetAsync($"accounts/{targetAccountId}/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

}

