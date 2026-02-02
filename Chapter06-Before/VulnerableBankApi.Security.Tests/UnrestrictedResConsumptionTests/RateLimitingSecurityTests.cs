using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace VulnerableBankApi.Security.Tests;

public class RateLimitingSecurityTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task GetAccount_RateLimitingEnabled_ShouldReturn_TooManyRequests(string targetAccountId)
    {
        // Arrange
        var requestingUserId = "52d43d2f-8859-4c38-9357-e1f41e21b3f8"; 
        var requestingUserIdAccounts = new List<string> { "7ca61183-46cf-4e8d-b506-9b787e45f2d9" };
        var token = TestTokenGenerator.GenerateJwtToken(requestingUserId, requestingUserIdAccounts);

        // Based on your configuration:
        // - Permit limit: 4
        // - Queue limit: 2
        // - Window: 12 seconds
        // To trigger OnRejected, we need more than 6 requests (4 permit + 2 queue)
        const int successfulRequests = 4; // These should succeed
        const int queuedRequests = 2;     // These will be queued
        const int rejectedRequests = 4;   // These should trigger OnRejected
        const int totalRequests = successfulRequests + queuedRequests + rejectedRequests;

        var responses = new List<HttpResponseMessage>();

        // Act
        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Send multiple GET requests rapidly to trigger rate limiting
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < totalRequests; i++)
        {
            // Use async to send requests as quickly as possible
            tasks.Add(_httpClient.GetAsync($"/accounts/{targetAccountId}"));
        }

        // Wait for all requests to complete
        var results = await Task.WhenAll(tasks);
        responses.AddRange(results);

        // Assert
        // Count successful responses (should be approximately 4-6, depending on timing)
        // var successCount = responses.Count(r => r.IsSuccessStatusCode);
        // successCount.Should().BeGreaterThanOrEqualTo(successfulRequests)
        //     .And.BeLessThanOrEqualTo(successfulRequests + queuedRequests,
        //     "first 4 requests should succeed, next 2 might be queued");

        // Count rate-limited responses
        var rateLimitedResponses = responses.Where(r =>
            r.StatusCode == HttpStatusCode.TooManyRequests).ToList();

        rateLimitedResponses.Should().NotBeEmpty("some requests should be rate limited");

    }

    [Theory]
    [InlineData("7ca61183-46cf-4e8d-b506-9b787e45f2d9")]
    public async Task GetAccount_RateLimitingEnabled_ShouldReturnRateLimitHeaders(string targetAccountId)
    {
        // Arrange
        var requestingUserId = "52d43d2f-8859-4c38-9357-e1f41e21b3f8"; 
        var requestingUserIdAccounts = new List<string> { "7ca61183-46cf-4e8d-b506-9b787e45f2d9" };
        var token = TestTokenGenerator.GenerateJwtToken(requestingUserId, requestingUserIdAccounts);

        // Based on your configuration:
        // - Permit limit: 4
        // - Queue limit: 2
        // - Window: 12 seconds
        // To trigger OnRejected, we need more than 6 requests (4 permit + 2 queue)
        const int successfulRequests = 4; // These should succeed
        const int queuedRequests = 2;     // These will be queued
        const int rejectedRequests = 4;   // These should trigger OnRejected
        const int totalRequests = successfulRequests + queuedRequests + rejectedRequests;
        var responses = new List<HttpResponseMessage>();

        // Act
        var _httpClient = _factory.CreateClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Send multiple GET requests rapidly to trigger rate limiting
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < totalRequests; i++)
        {
            // Use async to send requests as quickly as possible
            tasks.Add(_httpClient.GetAsync($"/accounts/{targetAccountId}"));
        }

        // Wait for all requests to complete
        var results = await Task.WhenAll(tasks);
        responses.AddRange(results);

        var rateLimitedResponses = responses.Where(r =>
            r.Headers.Contains("Retry-After")).ToList();

        rateLimitedResponses.Should().NotBeEmpty("OnRejected should set Retry-After header");

    }

}