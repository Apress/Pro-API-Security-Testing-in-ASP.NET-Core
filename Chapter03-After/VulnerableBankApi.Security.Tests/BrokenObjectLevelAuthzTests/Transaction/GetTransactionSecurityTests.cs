using System.Net;
using FluentAssertions;
using System.Net.Http.Headers;

namespace VulnerableBankApi.Security.Tests;

public class GetTransactionSecurityTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;
    
    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task Get_OtherCustomersTransaction_ReturnsAccessDenied(string targetAccountId)
    {
        // Arrange
        // fixed user
        var requestingUserId = "52d43d2f-8859-4c38-9357-e1f41e21b3f8"; 
        // this user has access to the account with id "0f7be158-31e9-4a34-be0c-818a6468ffa2"
        var requestingUserIdAccounts = new List<string> { "0f7be158-31e9-4a34-be0c-818a6468ffa2" };
        var token = TestTokenGenerator.GenerateJwtToken(requestingUserId, requestingUserIdAccounts);
        
        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        // try to access account of another user
        var response = await _httpClient.GetAsync($"accounts/{targetAccountId}/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

}

