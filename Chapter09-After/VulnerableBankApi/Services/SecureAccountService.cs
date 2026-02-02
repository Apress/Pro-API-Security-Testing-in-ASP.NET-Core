using System.Security.Claims;
using VulnerableBankApi.Dto;
using VulnerableBankApi.Services;

public class SecureAccountService : ISecureAccountService
{
    private readonly ISecureAccountRepository _repository;

    private readonly ILogger<SecureAccountService> _logger;
    
    public SecureAccountService(
        ISecureAccountRepository repository,
        ILogger<SecureAccountService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<AccountResponseDto?> GetAccountAsync(Guid id, ClaimsPrincipal user)
    {
        // Extract user information
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
        {
            _logger.LogWarning("Invalid user claims when accessing account {AccountId}", id);
            throw new UnauthorizedAccessException("Invalid user claims");
        }
        
        // Validate input
        if (id == Guid.Empty)
        {
            _logger.LogWarning("Invalid account ID requested by user {UserId}", userId);
            throw new ArgumentException("Invalid account ID", nameof(id));
        }
        
        // Get account with only authorized fields from the database
        var account = await _repository.GetAccountWithPermissionsAsync(id, userId, role);
        
        if (account == null)
        {
            return null;
        }
        
        // Log which fields were accessed
        var accessedFields = GetAccessedFields(account);
        
        return account;
    }
    
    private List<string> GetAccessedFields(AccountResponseDto account)
    {
        var fields = new List<string> { "Id", "AccountNumber", "AccountType", "CreatedAt", "LastModified" };
        
        if (account.AvailableBalance.HasValue) fields.Add("AvailableBalance");
        if (account.CurrentBalance.HasValue) fields.Add("CurrentBalance");
        if (account.CreditLimit.HasValue) fields.Add("CreditLimit");
        if (account.InterestRate.HasValue) fields.Add("InterestRate");
        if (!string.IsNullOrEmpty(account.RoutingNumber)) fields.Add("RoutingNumber");
        if (!string.IsNullOrEmpty(account.TaxIdentificationNumber)) fields.Add("TaxIdentificationNumber");
        
        return fields;
    }
}