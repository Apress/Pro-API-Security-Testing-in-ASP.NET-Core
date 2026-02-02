
namespace VulnerableBankApi.Services;

public interface IExternalVerificationService
{
    Task<ExchangeRateResponse?> GetExchangeRateAsync(string apiUrl);
    Task<bool> NotifyPaymentProcessorAsync(string merchantWebhookUrl, string transactionId, decimal amount);
}