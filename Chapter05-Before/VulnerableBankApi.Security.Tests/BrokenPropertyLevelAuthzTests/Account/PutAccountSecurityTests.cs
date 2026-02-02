using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using VulnerableBankApi.Dto;

namespace VulnerableBankApi.Security.Tests;

public class PutAccountSecurityTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task Update_AccountInterestRate_ReturnsForbidden(string accountId)
    {
        // Arrange
        var requestingUserId = "52d43d2f-8859-4c38-9357-e1f41e21b3f8"; 
        var role = "Customer"; // Simulate a customer role
        var token = TestTokenGenerator.GenerateJwtToken(requestingUserId, role);

        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var account = await _httpClient.GetFromJsonAsync<AccountResponseDto>($"accounts/{accountId}");

        // Act        
        var updateResponse = await _httpClient.PutAsJsonAsync($"/accounts/{accountId}",
            new AccountUpdateDto(
                AvailableBalance: account?.AvailableBalance + 1000,
                CurrentBalance: account?.CurrentBalance + 1000,
                CreditLimit: account?.CreditLimit,
                // Try to update the interest rate (sensitive info)
                InterestRate: 0.50m
            ));


        // Only try to read the content if the response is successful
        AccountResponseDto? updatedAccount = null;
        if (updateResponse.IsSuccessStatusCode)
        {
            updatedAccount = await updateResponse.Content.ReadFromJsonAsync<AccountResponseDto>();

            // Assert
            updatedAccount?.InterestRate.Should().BeLessThanOrEqualTo(0.25m);
        }

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

}

