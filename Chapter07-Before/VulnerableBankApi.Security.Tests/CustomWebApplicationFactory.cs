using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using VulnerableBankApi.Data;
using VulnerableBankApi.Models;

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresSql = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresSql.StartAsync();
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseNpgsql(_postgresSql.GetConnectionString())
            .Options;

        using var db = new BankDbContext(options);
        await db.Database.MigrateAsync();
        // await db.Database.EnsureCreatedAsync();
        
        // Manual seeding
        if (!await db.Accounts.AnyAsync(a => a.Id == new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9")))
        {
            await db.Accounts.AddAsync(new Account
            {
                Id = new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9"),
                AccountNumber = "9AEF35F593",
                AvailableBalance = 0m,
                CreatedAt = new DateTime(2024, 11, 4, 3, 31, 55, 827, DateTimeKind.Utc).AddTicks(7239),
                CreditLimit = 1000m,
                CurrentBalance = 0m,
                InterestRate = 0.25m,
                LastModified = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                RoutingNumber = "026009593",
                Status = AccountStatus.Active,
                TaxIdentificationNumber = "0123456789",
                Type = AccountType.Checking,
                UserId = new Guid("52d43d2f-8859-4c38-9357-e1f41e21b3f8")
            });
        }

        if (!await db.Accounts.AnyAsync(a => a.Id == new Guid("72208569-3a8e-41d3-b49e-4d8ca12f605c")))
        {
            await db.Accounts.AddAsync(new Account
            {
                Id = new Guid("72208569-3a8e-41d3-b49e-4d8ca12f605c"),
                AccountNumber = "9AEF35F594",
                AvailableBalance = 0m,
                CreatedAt = new DateTime(2024, 11, 4, 3, 31, 55, 827, DateTimeKind.Utc).AddTicks(7239),
                CreditLimit = 1000m,
                CurrentBalance = 0m,
                InterestRate = 0.25m,
                LastModified = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                RoutingNumber = "026009593",
                Status = AccountStatus.Frozen,
                TaxIdentificationNumber = "0123456789",
                Type = AccountType.Checking,
                UserId = new Guid("52d43d2f-8859-4c38-9357-e1f41e21b3f8")
            });
        }

        if (!await db.Loans.AnyAsync(l => l.Id == new Guid("1ca61183-46cf-4e8d-b506-9b787e45f2d1")))
        {
            await db.Loans.AddAsync(new Loan
            {
                Id = new Guid("1ca61183-46cf-4e8d-b506-9b787e45f2d1"),
                AccountId = new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9"),
                UserId = new Guid("52d43d2f-8859-4c38-9357-e1f41e21b3f8"),
                RequestedAmount = 50000m,
                ApprovedAmount = null,
                InterestRate = null,
                TermMonths = null,
                Type = LoanType.Personal,
                Status = LoanStatus.Pending,
                Purpose = "Home renovation",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                LastModified = DateTime.UtcNow.AddDays(-5),
                CreditScore = 750,
                RiskLevel = "Medium"
            });
        }

        await db.SaveChangesAsync();        
    }

    public new async Task DisposeAsync()
    {
        await _postgresSql.StopAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });

        builder.ConfigureTestServices(serviceCollection =>
        {
            var descriptor = serviceCollection
                .SingleOrDefault(s =>
                    s.ServiceType == typeof(DbContextOptions<BankDbContext>));

            if (descriptor is not null)
            {
                serviceCollection.Remove(descriptor);
            }
            

            serviceCollection.AddDbContext<BankDbContext>(options =>
            {
                options.UseNpgsql(_postgresSql.GetConnectionString())
                .UseAsyncSeeding(async (context, _, cancellationToken) =>
                    {
                        var testAccount = await context.Set<Account>().FirstOrDefaultAsync(
                            a => a.Id == new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9"));

                        if (testAccount != null) return;
                        await context.Set<Account>().AddAsync(new Account
                        {
                            Id = new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9"),
                            AccountNumber = "9AEF35F593",
                            AvailableBalance = 0m,
                            CreatedAt = new DateTime(2024, 11, 4, 3, 31, 55, 827, DateTimeKind.Utc).AddTicks(7239),
                            CreditLimit = 1000m,
                            CurrentBalance = 0m,
                            InterestRate = 0.25m,
                            LastModified = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                            RoutingNumber = "026009593",
                            Status = 0,
                            TaxIdentificationNumber = "0123456789",
                            Type = AccountType.Checking,
                            UserId = new Guid("52d43d2f-8859-4c38-9357-e1f41e21b3f8")
                        }, cancellationToken);

                        var testLoan = await context.Set<Loan>().FirstOrDefaultAsync(a => a.Id == new Guid("1ca61183-46cf-4e8d-b506-9b787e45f2d1"), cancellationToken: cancellationToken);

                        if (testLoan != null) return;
                        await context.Set<Loan>().AddAsync(new Loan
                        {
                            Id = new Guid("1ca61183-46cf-4e8d-b506-9b787e45f2d1"),
                            AccountId = new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9"),
                            UserId = new Guid("52d43d2f-8859-4c38-9357-e1f41e21b3f8"),
                            RequestedAmount = 50000m,
                            ApprovedAmount = null,
                            InterestRate = null,
                            TermMonths = null,
                            Type = LoanType.Personal,
                            Status = LoanStatus.Pending,
                            Purpose = "Home renovation",
                            CreatedAt = DateTime.UtcNow.AddDays(-5),
                            LastModified = DateTime.UtcNow.AddDays(-5),
                            CreditScore = 750,
                            RiskLevel = "Medium"
                        }, cancellationToken);
                        await context.SaveChangesAsync(cancellationToken);
                    })
                    .UseSeeding((context, _) =>
                    {
                        var testAccount = context.Set<Account>().FirstOrDefault(
                            a => a.Id == new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9"));

                        if (testAccount != null) return;
                            context.Set<Account>().Add(
                                new Account
                                {
                                    Id = new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9"),
                                    AccountNumber = "9AEF35F593",
                                    AvailableBalance = 0m,
                                    CreatedAt = new DateTime(2024, 11, 4, 3, 31, 55, 827, DateTimeKind.Utc).AddTicks(7239),
                                    CreditLimit = 1000m,
                                    CurrentBalance = 0m,
                                    InterestRate = 0.25m,
                                    LastModified = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                                    RoutingNumber = "026009593",
                                    Status = 0,
                                    TaxIdentificationNumber = "0123456789",
                                    Type = AccountType.Checking,
                                    UserId = new Guid("52d43d2f-8859-4c38-9357-e1f41e21b3f8")
                                });

                            var testLoan = context.Set<Loan>().FirstOrDefaultAsync(
                                a => a.Id == new Guid("1ca61183-46cf-4e8d-b506-9b787e45f2d1"));

                        if (testLoan != null) return;
                            context.Set<Loan>().Add(
                                new Loan
                                {
                                    Id = new Guid("1ca61183-46cf-4e8d-b506-9b787e45f2d1"),
                                    AccountId = new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9"),
                                    UserId = new Guid("52d43d2f-8859-4c38-9357-e1f41e21b3f8"),
                                    RequestedAmount = 50000m,
                                    ApprovedAmount = null,
                                    InterestRate = null,
                                    TermMonths = null,
                                    Type = LoanType.Personal,
                                    Status = LoanStatus.Pending,
                                    Purpose = "Home renovation",
                                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                                    LastModified = DateTime.UtcNow.AddDays(-5),
                                    CreditScore = 750,
                                    RiskLevel = "Medium"
                                });
                            context.SaveChanges();
                    });
            }); 

            var authenticationBuilder = serviceCollection.AddAuthentication();
            authenticationBuilder.Services.Configure<AuthenticationOptions>(o =>
            {
                o.SchemeMap.Clear();
                ((IList<AuthenticationSchemeBuilder>)o.Schemes).Clear();
            });
            authenticationBuilder = serviceCollection.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = false;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8
                            .GetBytes("bAafd@A7d9#@F4*V!LHZs#ebKQrkE6pad2f3kj34c3dXy@") // NOTE: Hardcoded keys are a security risk, for demo purposes only
                        ),
                        ValidIssuer = "example.com",
                        ValidAudience = "example.com",
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true
                    };
                });                 
        });        

        builder.UseEnvironment("Development");
    }
}