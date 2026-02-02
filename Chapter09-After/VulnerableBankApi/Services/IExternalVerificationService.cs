using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace VulnerableBankApi.Services;

public interface IExternalVerificationService
{
    Task<ExchangeRateResponse?> GetExchangeRateAsync(string apiUrl);
    Task<bool> NotifyPaymentProcessorAsync(string merchantId, string merchantWebhookUrl, string transactionId, decimal amount);
}