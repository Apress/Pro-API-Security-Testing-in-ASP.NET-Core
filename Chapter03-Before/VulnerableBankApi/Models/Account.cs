using System.ComponentModel.DataAnnotations.Schema;

namespace VulnerableBankApi.Models;
public class Account
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string AccountNumber { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public required decimal Balance { get; set; }
    public required AccountType Type { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
}

public enum AccountType
{
    Checking,
    Savings,
    Investment
}