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

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

builder.Services.AddAuthentication(x =>
{
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options => {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters {
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("bAafd@A7d9#@F4*V!LHZs#ebKQrkE6pad2f3kj34c3dXy@")
            ),
            ValidateIssuerSigningKey = false,
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

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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

var accountsGroup = app.MapGroup("/accounts");

accountsGroup.MapGet("/{accountId}/transactions", async (Guid accountId, ITransactionService transactionService, ClaimsPrincipal user) =>
{
    // BOLA Vulnerability Fixed: Validate if the authenticated user owns this account by checking its claims
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

app.Run();

public partial class Program { }