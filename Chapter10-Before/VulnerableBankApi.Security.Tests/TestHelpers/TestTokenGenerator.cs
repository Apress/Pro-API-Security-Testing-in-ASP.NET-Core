using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class TestTokenGenerator
{
    private static readonly string DefaultSecurityKey = "bAafd@A7d9#@F4*V!LHZs#ebKQrkE6pad2f3kj34c3dXy@";

    public static string GenerateJwtToken(string userId)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultSecurityKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "example.com",
            audience: "example.com",
            claims: [
                new Claim(ClaimTypes.NameIdentifier, userId),
            ],
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }    

    public static string GenerateJwtToken(string userId, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultSecurityKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "example.com",
            audience: "example.com",
            claims: [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            ],
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }    


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

    public static string GenerateJwtToken(string userId, IEnumerable<string> accounts, string role)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultSecurityKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "example.com",
            audience: "example.com",
            claims: [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                new("Accounts", string.Join(",", accounts))
            ],
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }    


    public static string GenerateToken(string issuer, string audience, DateTime expires, string userId)
    {
        var key = Convert.FromBase64String(DefaultSecurityKey);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
            ],
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateTokenWithCustomKey(
        string userId,
        string customKey, 
        string issuer, 
        string audience, 
        DateTime expires)
    {
        var key = Encoding.UTF8.GetBytes(customKey);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
            ],
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateTokenWithCustomClaims(
        IDictionary<string, object> claims,
        string issuer,
        string audience,
        DateTime expires)
    {
        var key = Convert.FromBase64String(DefaultSecurityKey);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha512Signature);

        var tokenClaims = claims.Select(c => 
            new Claim(c.Key, c.Value.ToString() ?? string.Empty));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: tokenClaims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateTokenWithCustomNotBefore(
        string userId,
        string issuer,
        string audience,
        DateTime notBefore,
        DateTime expires)
    {
        var key = Convert.FromBase64String(DefaultSecurityKey);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
            ],
            notBefore: notBefore,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateTokenWithCustomExpiration(
        string userId,
        TimeSpan expiration)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultSecurityKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: "example.com",
            audience: "example.com",
            claims: [
                new Claim(ClaimTypes.NameIdentifier, userId)
            ],
            expires: now.Add(expiration),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }    

    public static string GenerateTokenWithNoExpiration(
        string userId)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultSecurityKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "example.com",
            audience: "example.com",
            claims: [
                new Claim(ClaimTypes.NameIdentifier, userId)
            ],
            //expires: null,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }    


}