using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using VulnerableBankApi.Dto;

namespace VulnerableBankApi.Security.Tests;

public class GetAccountSecurityTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task Get_AccountInterestRate_ReturnsNull(string targetAccountId)
    {
        // Arrange
        var requestingUserId = "52d43d2f-8859-4c38-9357-e1f41e21b3f8"; 
        var role = "CustomerRepresentative"; // Simulate a customer representative role
        var token = TestTokenGenerator.GenerateJwtToken(requestingUserId, role);

        // Act
        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var account = await _httpClient.GetFromJsonAsync<AccountResponseDto>($"accounts/{targetAccountId}");

        // Assert
        account.Should().NotBeNull();

        // Verify sensitive data is not exposed
        account?.TaxIdentificationNumber.Should().BeNull();
    }

}