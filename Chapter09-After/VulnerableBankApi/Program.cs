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

builder.Services.AddHttpClient<IExternalVerificationService, ExternalVerificationService>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
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


var externalGroup = app.MapGroup("/external").RequireAuthorization();

externalGroup.MapGet("/exchange-rate", async (string apiUrl,
    IExternalVerificationService verificationService) =>
{
    try
    {
        var exchangeRate = await verificationService.GetExchangeRateAsync(apiUrl);
        return exchangeRate is null ? Results.NotFound() : Results.Ok(exchangeRate);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error fetching exchange rate: {ex.Message}");
    }
})
.WithName("GetExchangeRate")
.WithOpenApi();

externalGroup.MapPost("/notify-payment", async (PaymentNotificationRequest request,
    IExternalVerificationService verificationService) =>
{
    try
    {        
        var success = await verificationService.NotifyPaymentProcessorAsync(
            request.MerchantId,
            request.MerchantWebhookUrl,
            request.TransactionId,
            request.Amount);
        
        return success 
            ? Results.Ok(new { status = "Payment notification sent", transactionId = request.TransactionId })
            : Results.StatusCode(StatusCodes.Status502BadGateway);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Payment notification error: {ex.Message}");
    }
})
.WithName("NotifyPaymentProcessor")
.WithOpenApi()
.RequireRateLimiting("fixed");  // Rate limiting is important for payment endpoints

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


app.Run();

public partial class Program { }