
using Microsoft.EntityFrameworkCore;
using VulnerableBankApi.Data;

namespace VulnerableBankApi.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BankDbContext>();
            dbContext.Database.Migrate();
            dbContext.Database.EnsureCreated();
        }        
    }

}