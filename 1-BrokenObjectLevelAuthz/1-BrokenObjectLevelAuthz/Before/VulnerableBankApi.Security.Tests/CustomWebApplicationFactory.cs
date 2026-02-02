using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Testcontainers.MsSql;
using Microsoft.EntityFrameworkCore;
using VulnerableBankApi.Data;

public class CustomWebApplicationFactory<TProgram>
    : WebApplicationFactory<Program>, IAsyncLifetime
{

    private readonly MsSqlContainer _msSql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-CU10-ubuntu-22.04")
        // .WithPassword("YourStrong@Passw0rd")
        // .WithHostname("localhost")
        .Build();

    public async Task InitializeAsync()
    {
        await _msSql.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _msSql.StopAsync();
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
            
            if (descriptor is not null) {
                serviceCollection.Remove(descriptor);
            }

            serviceCollection.AddDbContext<BankDbContext>(options => 
                {
                    options.UseSqlServer(_msSql.GetConnectionString());
                }
            );

            var authenticationBuilder = serviceCollection.AddAuthentication();
            authenticationBuilder.Services.Configure<AuthenticationOptions>(o =>
            {
                o.SchemeMap.Clear();
                ((IList<AuthenticationSchemeBuilder>) o.Schemes).Clear();
            });            
            authenticationBuilder = serviceCollection.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }).AddJwtBearer(x => {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = false;
                    x.TokenValidationParameters = new TokenValidationParameters {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8
                            .GetBytes("bAafd@A7d9#@F4*V!LHZs#ebKQrkE6pad2f3kj34c3dXy@") // NOTE: Hardcoded keys are a security risk, for demo purposes only
                        ),
                        ValidIssuer = "example.com",
                        ValidAudience = "example.com",                               
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    };
                });
        });        

        builder.UseEnvironment("Development");
    }
}