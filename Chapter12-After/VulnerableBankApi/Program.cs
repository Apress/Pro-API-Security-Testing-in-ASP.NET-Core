using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using FluentValidation;
using Serilog;
using System.Security.Claims;
using VulnerableBankApi.Data;
using VulnerableBankApi.Services;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VulnerableBankApi.Extensions;
using VulnerableBankApi.Models;
using VulnerableBankApi.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseAsyncSeeding(async (context, _, cancellationToken) =>
    {
        var testAccount = await context.Set<Account>().FirstOrDefaultAsync(
            a => a.Id == new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9"));

        if (testAccount != null) return;
        await context.Set<Account>().AddAsync(
            new Account
            {
                Id = new Guid("7ca61183-46cf-4e8d-b506-9b787e45f2d9"),
                AccountNumber = "9AEF35F593",
                AvailableBalance = 2000m,
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
        await context.SaveChangesAsync(cancellationToken);
    })
    .UseAsyncSeeding(async (context, _, cancellationToken) =>
    {
        var testAccount = await context.Set<Account>().FirstOrDefaultAsync(
            a => a.Id == new Guid("712e25a5-4549-4673-b0f7-d77e36c4ea84"));

        if (testAccount != null) return;
        await context.Set<Account>().AddAsync(
            new Account
            {
                Id = new Guid("712e25a5-4549-4673-b0f7-d77e36c4ea84"),
                AccountNumber = "9AEF35F594",
                AvailableBalance = 3000m,
                CreatedAt = new DateTime(2024, 11, 4, 3, 31, 55, 827, DateTimeKind.Utc).AddTicks(7239),
                CreditLimit = 2000m,
                CurrentBalance = 0m,
                InterestRate = 0.25m,
                LastModified = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                RoutingNumber = "026009593",
                Status = AccountStatus.Frozen,
                TaxIdentificationNumber = "0123456781",
                Type = AccountType.Savings,
                UserId = new Guid("52d43d2f-8859-4c38-9357-e1f41e21b3f8")
            });
        await context.SaveChangesAsync(cancellationToken);
    })
    .UseSeeding((context, _) =>
    {
        var testAccount = context.Set<Account>().FirstOrDefault(
            a => a.Id == new Guid("712e25a5-4549-4673-b0f7-d77e36c4ea84"));

        if (testAccount != null) return;
            context.Set<Account>().Add(
                new Account
                {
                    Id = new Guid("712e25a5-4549-4673-b0f7-d77e36c4ea84"),
                    AccountNumber = "9AEF35F594",
                    AvailableBalance = 2000m,
                    CreatedAt = new DateTime(2024, 11, 4, 3, 31, 55, 827, DateTimeKind.Utc).AddTicks(7239),
                    CreditLimit = 2000m,
                    CurrentBalance = 0m,
                    InterestRate = 0.25m,
                    LastModified = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    RoutingNumber = "026009593",
                    Status = AccountStatus.Frozen,
                    TaxIdentificationNumber = "0123456780",
                    Type = AccountType.Checking,
                    UserId = new Guid("52d43d2f-8859-4c38-9357-e1f41e21b3f8")
                });         
        context.SaveChanges();
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
                    AvailableBalance = 3000m,
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
        context.SaveChanges();
    })

);


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ISecureAccountService, SecureAccountService>();
builder.Services.AddScoped<ISecureAccountRepository, SecureAccountRepository>();

builder.Services.AddScoped<ITransactionService, TransactionService>();
//builder.Services.AddScoped<IThirdPartyIntegrationService, ThirdPartyIntegrationService>();

// Use secure third-party integration service that blocks GraphQL introspection
builder.Services.AddHttpClient<IThirdPartyIntegrationService, SecureThirdPartyIntegrationService>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    // VULNERABILITY: Disabling SSL certificate validation (dangerous!)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
    });

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options => {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters {
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("bAafd@A7d9#@F4*V!LHZs#ebKQrkE6pad2f3kj34c3dXy@")
            ),
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = false,
            
            ValidIssuer = "example.com",
            ValidAudience = "example.com",                        
            ValidateIssuer = false,
            ValidateAudience = false,

            ValidateLifetime = false,
            RequireExpirationTime = false
        };
});

builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Vulnerable Bank API", Description = "a vulnerable banking minimal API", Version = "v1" });
});    

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 4;
        opt.Window = TimeSpan.FromSeconds(12);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        // Custom rejection handling logic
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers["Retry-After"] = "60";

        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken);
    };    
});



builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("LoanOfficerPolicy", policy =>
        policy.RequireRole("LoanOfficer")); // Require specific role
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.ApplyMigrations();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

var accountsGroup = app.MapGroup("/accounts").RequireAuthorization();

accountsGroup.MapGet("/{accountId}/transactions", async (Guid accountId, ITransactionService transactionService, ClaimsPrincipal user) =>
{
    var userClaims = user.FindFirst("Accounts")?.Value;
    if (string.IsNullOrEmpty(userClaims))
        return Results.Forbid();

    var accountIds = userClaims.Split(',').Select(Guid.Parse);
    if (!accountIds.Contains(accountId))
        return Results.Forbid();

    var transactions = await transactionService.GetAccountTransactionsAsync(accountId);
    return Results.Ok(transactions);
})
.WithName("GetAccountTransactions")
.WithOpenApi();

accountsGroup.MapGet("/{accountId}", async (Guid accountId,
    [FromServices] ISecureAccountService accountService,
    ClaimsPrincipal user) =>
{
    var userClaims = user.FindFirst("Accounts")?.Value;
    if (string.IsNullOrEmpty(userClaims))
        return Results.Forbid();

    var accountIds = userClaims.Split(',').Select(Guid.Parse);
    if (!accountIds.Contains(accountId))
        return Results.Forbid();

    var account = await accountService.GetAccountAsync(accountId, user);
    return account is null ? Results.NotFound() : TypedResults.Ok(account);
})
.WithName("GetAccount")
.WithOpenApi()
.RequireRateLimiting("fixed");

accountsGroup.MapPut("/{accountId}", async (Guid accountId,
    [FromBody] AccountUpdateDto accountUpdateDto,
    [FromServices] IAccountService accountService,
    ClaimsPrincipal user) =>
{
    var account = await accountService.UpdateAccountAsync(accountId, accountUpdateDto);
    return Results.Ok(account);
})
.RequireAuthorization("LoanOfficerPolicy")
.WithName("UpdateAccount")
.WithOpenApi();


// VULNERABLE: Transfer endpoint without business flow restrictions
accountsGroup.MapPost("/{accountId}/transactions", async (Guid accountId, ITransactionService transactionService, ClaimsPrincipal user,
    TransactionDto transactionDto) =>
{
    // VULNERABILITY: No rate limiting on transfers
    // VULNERABILITY: No velocity checks
    // VULNERABILITY: No fraud detection
    // VULNERABILITY: No daily/monthly limits
    
    var userClaims = user.FindFirst("Accounts")?.Value;
    if (string.IsNullOrEmpty(userClaims))
        return Results.Forbid();

    var accountIds = userClaims.Split(',').Select(Guid.Parse);
    
    // Basic authorization check only
    if (!accountIds.Contains(transactionDto.FromAccountId))
        return Results.Forbid();

    var result = await transactionService.ProcessTransferAsync(transactionDto);
    
    return result != null 
        ? Results.Ok(result) 
        : Results.BadRequest("Transfer failed");
})
.RequireAuthorization()
.WithName("ProcessTransfer")
.WithOpenApi();

// VULNERABILITY: New endpoint that uses unsafe third-party APIs
accountsGroup.MapGet("/{accountId}/creditscore", async (
                        Guid accountId,
                        IAccountService accountService,
                        IThirdPartyIntegrationService thirdPartyService,
                        ClaimsPrincipal user) =>
{
    var account = await accountService.GetAccountAsync(accountId);
    if (account is null) return Results.NotFound();
    
    // VULNERABILITY: Sending sensitive data to third-party API over HTTP
    var creditScore = await thirdPartyService.GetCreditScoreAsync(account.TaxIdentificationNumber ?? "");
    
    return TypedResults.Ok(new { AccountNumber = accountId, CreditScore = creditScore });
})
.WithName("GetAccountCreditScore")
.WithOpenApi()
.RequireRateLimiting("fixed");

// VULNERABILITY: Endpoint that exposes GraphQL schema information
accountsGroup.MapPost("/graphql", async (
                        [FromBody] string query,
                        IThirdPartyIntegrationService thirdPartyService,
                        ClaimsPrincipal user) =>
{
    // VULNERABILITY: Exposing third-party GraphQL schema
    var schema = await thirdPartyService.ProcessGraphQLQueryAsync(query);
    return TypedResults.Ok(schema);
})
.WithName("GetGraphQLSchema")
.WithOpenApi()
.RequireRateLimiting("fixed");

app.Run();

public partial class Program { }