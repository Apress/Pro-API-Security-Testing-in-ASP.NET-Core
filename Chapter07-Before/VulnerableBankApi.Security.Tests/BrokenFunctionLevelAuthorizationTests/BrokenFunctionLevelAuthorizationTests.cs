using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using VulnerableBankApi.Dto;
using VulnerableBankApi.Models;

namespace VulnerableBankApi.Security.Tests;

public class BrokenFunctionLevelAuthorizationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Theory]
    [InlineData("1ca61183-46cf-4e8d-b506-9b787e45f2d1",
                "52d43d2f-8859-4c38-9357-e1f41e21b3f8",
                "7ca61183-46cf-4e8d-b506-9b787e45f2d9",
                "Customer",
                50000)]      // Customer approving own $50K loan
    public async Task PutAccountLoan_UnauthorizedApproval_ShouldReturnForbidden(
        string loanId, string userId, string accountId, string role, decimal loanAmount)
    {
        // Arrange
        var requestingUserIdAccounts = new List<string> { "7ca61183-46cf-4e8d-b506-9b787e45f2d9" };

        var token = TestTokenGenerator.GenerateJwtToken(userId, requestingUserIdAccounts, role);
        var loanUpdateDto = new
        {
            approvedAmount = loanAmount,
            interestRate = 5.5m,
            termMonths = 60,
            status = 2, // LoanStatus.Approved
            approvalNotes = $"Unauthorized approval attempt by {role}"
        };

        // Act
        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new StringContent(
            JsonSerializer.Serialize(loanUpdateDto),
            Encoding.UTF8,
            "application/json");

        // Act - Non-loan officer attempting to approve loan via PUT endpoint
        var response = await _httpClient.PutAsync(
            $"accounts/{accountId}/loans/{loanId}",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

    }
    
    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9","Teller")]
    public async Task UpdateAccount_UnauthorizedFreeze_ReturnsForbidden(string accountId, string role)
    {
        // Arrange
        var requestingUserId = "52d43d2f-8859-4c38-9357-e1f41e21b3f8"; 
        var requestingUserIdAccounts = new List<string> { "7ca61183-46cf-4e8d-b506-9b787e45f2d9" };
        var token = TestTokenGenerator.GenerateJwtToken(requestingUserId, requestingUserIdAccounts, role);

        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var account = await _httpClient.GetFromJsonAsync<AccountResponseDto>($"accounts/{accountId}");

        // Act        
        var updateResponse = await _httpClient.PutAsJsonAsync($"/accounts/{accountId}",
            new AccountUpdateDto(
                AvailableBalance: account?.AvailableBalance,
                CurrentBalance: account?.CurrentBalance,
                CreditLimit: account?.CreditLimit,
                InterestRate: account?.InterestRate,
                Status: AccountStatus.Frozen
            ));

        // Only try to read the content if the response is successful
        // AccountResponseDto? updatedAccount = null;
        // if (updateResponse.IsSuccessStatusCode)
        // {
        //     updatedAccount = await updateResponse.Content.ReadFromJsonAsync<AccountResponseDto>();
        //     // Assert
        //     updatedAccount?.Status.Should().Be(AccountStatus.Active, "Teller should not be able to freeze the account");
        // }

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


}