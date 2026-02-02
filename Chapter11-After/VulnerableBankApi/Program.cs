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
using System.Linq;
using Microsoft.AspNetCore.Builder;
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

// VULNERABILITY: Multiple Swagger documents for different versions without proper management
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Vulnerable Bank API",
        Description = "Legacy API - Deprecated but still active",
        Version = "v1"
    });

    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Vulnerable Bank API",
        Description = "Current production API",
        Version = "v2"
    });

    c.SwaggerDoc("v3-beta", new OpenApiInfo
    {
        Title = "Vulnerable Bank API Beta",
        Description = "Beta features - Not for production use",
        Version = "v3-beta"
    });
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ISecureAccountService, SecureAccountService>();
builder.Services.AddScoped<ISecureAccountRepository, SecureAccountRepository>();

builder.Services.AddScoped<ITransactionService, TransactionService>();

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

// Soft-disable deprecated and beta API versions application-wide.
// This ensures deprecated routes are not active and helps tests (and security checks)
// detect only the current API surface. If you want to allow deprecated versions
// locally for development, change this to honor an environment flag.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;

    // Block /v1/* (deprecated) and /v3-beta/* (beta)
    if (path.StartsWith("/v1/", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/v1", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/v3-beta/", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/v3-beta", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status410Gone;
        context.Response.Headers["Deprecation"] = "true";
        context.Response.Headers["Link"] = "</docs/migration>; rel=\"migrate\"";
        await context.Response.WriteAsync("This API version is deprecated. Use /v2/.");
        return;
    }

    await next();
});

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

var accountsGroupV1 = app.MapGroup("/v1/accounts").RequireAuthorization().WithTags("v1").WithOpenApi();
var accountsGroupV2 = app.MapGroup("/v2/accounts").RequireAuthorization().WithTags("v2").WithOpenApi();
var accountsGroupV3Beta = app.MapGroup("/v3-beta/accounts").WithTags("v3-beta").WithOpenApi();

accountsGroupV1.MapGet("/{accountId}", async (Guid accountId,
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
.WithName("GetAccountV1")
.RequireRateLimiting("fixed");

accountsGroupV2.MapGet("/{accountId}", async (Guid accountId,
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
.WithName("GetAccountV2")
.RequireRateLimiting("fixed");

accountsGroupV3Beta.MapGet("/{accountId}", async (Guid accountId,
    [FromServices] ISecureAccountService accountService,
    ClaimsPrincipal user) =>
{
    var account = await accountService.GetAccountAsync(accountId, user);
    return account is null ? Results.NotFound() : TypedResults.Ok(account);
})
.WithName("GetAccountV3Beta")
.RequireRateLimiting("fixed");


app.Run();

public partial class Program { }