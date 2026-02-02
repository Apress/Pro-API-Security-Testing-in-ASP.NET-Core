using Microsoft.EntityFrameworkCore;
using VulnerableBankApi.Models;

namespace VulnerableBankApi.Data;
public class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    // protected override void OnModelCreating(ModelBuilder modelBuilder)
    // {
    //     modelBuilder.Entity<Account>(entity =>
    //     {
    //         entity.HasKey(e => e.Id);
    //         entity.HasIndex(e => e.AccountNumber).IsUnique();
    //         entity.Property(e => e.Id).ValueGeneratedNever(); // Since we're generating GUIDs in code
    //     });

    //     modelBuilder.Entity<Transaction>(entity =>
    //     {
    //         entity.HasKey(e => e.Id);
    //         entity.HasOne<Account>()
    //             .WithMany()
    //             .HasForeignKey(t => t.FromAccountId)
    //             .OnDelete(DeleteBehavior.Restrict);

    //         entity.HasOne<Account>()
    //             .WithMany()
    //             .HasForeignKey(t => t.ToAccountId)
    //             .OnDelete(DeleteBehavior.Restrict);

    //         entity.Property(e => e.Id).ValueGeneratedNever(); // Since we're generating GUIDs in code
    //     });

    //     modelBuilder.Entity<Account>().HasData(
    //         new Account
    //         {
    //             Id = new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9"),
    //             AccountNumber = "9AEF35F593",
    //             AvailableBalance = 0m,
    //             CreatedAt = new DateTime(2024, 11, 4, 3, 31, 55, 827, DateTimeKind.Utc).AddTicks(7239),
    //             CreditLimit = 1000m,
    //             CurrentBalance = 0m,
    //             InterestRate = 0.25m,
    //             LastModified = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
    //             RoutingNumber = "026009593",
    //             Status = 0,
    //             TaxIdentificationNumber = "0123456789",
    //             Type = AccountType.Checking,
    //             UserId = new Guid("52d43d2f-8859-4c38-9357-e1f41e21b3f8")            }
    //     );

    // }
}