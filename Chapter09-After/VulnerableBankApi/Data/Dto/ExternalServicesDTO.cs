// DTOs for the service
public record ExchangeRateResponse(string Currency, decimal Rate, DateTime Timestamp);
public record PaymentNotificationRequest(string MerchantId, string MerchantWebhookUrl, string TransactionId, decimal Amount);
public record PaymentWebhookPayload(
    string TransactionId, 
    decimal Amount, 
    string Status, 
    DateTime Timestamp,
    string InternalTransactionRef,  // Leaks internal reference
    string ProcessingNode,           // Leaks infrastructure info
    string DatabaseShard             // Leaks database architecture
);