// File: VulnerableBankApi\Models\Loan.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace VulnerableBankApi.Models;

public class Loan
{
    public required Guid Id { get; set; }
    public required Guid AccountId { get; set; }
    public Guid? UserId { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public required decimal RequestedAmount { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ApprovedAmount { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal? InterestRate { get; set; }
    
    public int? TermMonths { get; set; }
    public required LoanType Type { get; set; }
    public required LoanStatus Status { get; set; }
    public string? Purpose { get; set; }
    public string? ApprovalNotes { get; set; }
    public string? RejectionReason { get; set; }
    
    // Tracking fields
    public required DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? DisbursedAt { get; set; }
    public DateTime LastModified { get; set; }
    
    // Risk assessment
    public decimal? CreditScore { get; set; }
    public string? RiskLevel { get; set; } // Low, Medium, High, Critical
    
    // Navigation property
    public virtual Account? Account { get; set; }
}

public enum LoanType
{
    Personal,
    Mortgage,
    Auto,
    Business,
    StudentLoan,
    CreditCard,
    HomeEquity,
    Consolidation
}

public enum LoanStatus
{
    Pending,
    UnderReview,
    Approved,
    Rejected,
    Disbursed,
    Active,
    Delinquent,
    Default,
    Closed,
    Cancelled
}
