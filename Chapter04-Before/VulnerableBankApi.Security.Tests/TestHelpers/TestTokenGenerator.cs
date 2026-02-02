using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class TestTokenGenerator
{
    private static readonly string DefaultSecurityKey = "bAafd@A7d9#@F4*V!LHZs#ebKQrkE6pad2f3kj34c3dXy@";
    
    public static string GenerateJwtToken(string userId, IEnumerable<string> accounts)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultSecurityKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("Accounts", string.Join(",", accounts))
        };

        var token = new JwtSecurityToken(
            issuer: "example.com",
            audience: "example.com",
            claims: claims,
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateTokenWithCustomExpiration(
        string userId,
        TimeSpan expiration,
        IEnumerable<string> accounts)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultSecurityKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: "example.com",
            audience: "example.com",
            claims: [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new("Accounts", string.Join(",", accounts))
            ],
            expires: now.Add(expiration),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }    

    public static string GenerateTokenWithNoExpiration(
        string userId,
        IEnumerable<string> accounts)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultSecurityKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "example.com",
            audience: "example.com",
            claims: [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new("Accounts", string.Join(",", accounts))
            ],
            expires: null,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }    

}