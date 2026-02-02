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

        var optionsBuilder = new DbContextOptionsBuilder<BankDbContext>();
        optionsBuilder.UseNpgsql(_postgresSql.GetConnectionString());

        // Apply migrations & seed the accounts used in tests
        using var context = new BankDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        var sourceId = Guid.Parse("7ca61183-46cf-4e8d-b506-9b787e45f2d9");
        var targetId = Guid.Parse("712e25a5-4549-4673-b0f7-d77e36c4ea84");

        if (await context.Accounts.FindAsync(sourceId) is null)
        {
            context.Accounts.Add(new Account {
                Id = sourceId,
                AccountNumber = "9AEF35F593",
                AvailableBalance = 1000m,
                CurrentBalance = 1000m,
                CreditLimit = 1000m,
                RoutingNumber = "026009593",
                Status = 0,
                TaxIdentificationNumber = "0123456789",
                Type = AccountType.Checking,
                UserId = Guid.Parse("52d43d2f-8859-4c38-9357-e1f41e21b3f8"),
                CreatedAt = DateTime.UtcNow
            });
        }

        if (await context.Accounts.FindAsync(targetId) is null)
        {
            context.Accounts.Add(new Account {
                Id = targetId,
                AccountNumber = "9AEF35F594",
                AvailableBalance = 1000m,
                CurrentBalance = 1000m,
                CreditLimit = 2000m,
                RoutingNumber = "026009593",
                Status = AccountStatus.Frozen, // match your model enum if needed
                TaxIdentificationNumber = "0123456781",
                Type = AccountType.Savings,
                UserId = Guid.Parse("52d43d2f-8859-4c38-9357-e1f41e21b3f8"),
                CreatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();        
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

        builder.ConfigureServices(serviceCollection =>
        {

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
                    options.UseNpgsql(_postgresSql.GetConnectionString());
                }
            );

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

        // Run tests against a production-like environment so security checks
        // (Swagger exposure, deprecated endpoints) reflect production behavior.
        builder.UseEnvironment("Production");
    }
}