using System.ComponentModel.DataAnnotations.Schema;

namespace VulnerableBankApi.Models;
public class Transaction
{
    public required Guid Id { get; set; }
    public required Guid FromAccountId { get; set; }
    public required Guid ToAccountId { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public required decimal Amount { get; set; }
    public required DateTime Timestamp { get; set; }
}