using System.ComponentModel.DataAnnotations.Schema;

namespace VulnerableBankApi.Models;
public class Account
{
    public required Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string AccountNumber { get; set; }
    public required string RoutingNumber { get; set; }
    public required string TaxIdentificationNumber   { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? AvailableBalance { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public required decimal CurrentBalance { get; set; } = 0;
    [Column(TypeName = "decimal(18,2)")]
    public decimal? CreditLimit { get; set; } = 1000;

    [Column(TypeName = "decimal(2,2)")]
    public decimal? InterestRate { get; set; } = 0.025m;

    public required AccountType Type { get; set; }

    public required AccountStatus Status { get; set; }

    public required DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }    
    
}

public enum AccountType
{
    Checking,
    Savings,
    Investment,
    MoneyMarket,
    FixedDeposit
}

public enum AccountStatus
{
    Active,
    Frozen,
    Closed,
    UnderReview
}
