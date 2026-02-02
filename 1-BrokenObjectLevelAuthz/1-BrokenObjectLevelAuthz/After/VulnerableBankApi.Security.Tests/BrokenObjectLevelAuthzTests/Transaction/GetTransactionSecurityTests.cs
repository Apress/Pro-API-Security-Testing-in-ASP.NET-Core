using System.Net;
using FluentAssertions;
using System.Net.Http.Headers;

namespace VulnerableBankApi.Security.Tests;

public class GetTransactionSecurityTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task Get_OtherCustomersTransaction_ReturnsAccessDenied()
    {
        // Arrange
        var userId1Token = TestTokenGenerator.GenerateJwtToken("52d43d2f-8859-4c38-9357-e1f41e21b3f8");
        var userId2Token = TestTokenGenerator.GenerateJwtToken("7ca61183-46cf-4e8d-b506-9b787e45f2d9");

        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userId1Token);        
     
        // Act
        var response = await _httpClient.GetAsync($"accounts/7ca61183-46cf-4e8d-b506-9b787e45f2d9/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

    }

}

