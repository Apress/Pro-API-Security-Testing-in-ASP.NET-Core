using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using VulnerableBankApi.Dto;

namespace VulnerableBankApi.Security.Tests.UnrestrictedBusinessFlows;

public class UnrestrictedBusinessFlowTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9", "712e25a5-4549-4673-b0f7-d77e36c4ea84", "52d43d2f-8859-4c38-9357-e1f41e21b3f8")]
    public async Task TransferEndpoint_AllowsUnlimitedTransfersWithoutVelocityChecks(string sourceAccountId, string targetAccountId, string userId)
    {
        // Arrange
        var client = _factory.CreateClient();
        var accounts = new List<string> { sourceAccountId };

        var token = TestTokenGenerator.GenerateJwtToken(userId, accounts, "Customer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


        // Act - Perform multiple rapid transfers (simulating automated attack)
        var transferTasks = new List<Task<HttpResponseMessage>>();
        var numberOfTransfers = 50; // Excessive number of transfers in rapid succession
        var amountPerTransfer = 10m;

        // VULNERABILITY: API allows unlimited rapid transfers without any velocity checks
        for (int i = 0; i < numberOfTransfers; i++)
        {
            var transfer = new TransactionDto(
                Guid.Parse(sourceAccountId),
                Guid.Parse(targetAccountId),
                amountPerTransfer
            );

            var request = new HttpRequestMessage(HttpMethod.Post, $"/accounts/{sourceAccountId}/transactions")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(transfer),
                    Encoding.UTF8,
                    "application/json"
                )
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            transferTasks.Add(client.SendAsync(request));
        }

        var responses = await Task.WhenAll(transferTasks);

        // Assert - All transfers succeed without any rate limiting or velocity checks
        var successfulTransfers = responses.Count(r => r.IsSuccessStatusCode);

        // VULNERABILITY DEMONSTRATED: 
        // In a secure system, there should be limits on:
        // 1. Number of transfers per day/hour
        // 2. Total amount transferred per day
        // 3. Rapid successive transfers (velocity)

        successfulTransfers.Should().BeLessThan(10,
            "API6:2023 vulnerability - allows unlimited transfers without business flow restrictions");
    }
}
