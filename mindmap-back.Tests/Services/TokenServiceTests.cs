using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Services;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace mindmap_back.Tests.Services;

public class TokenServiceTests
{
    [Fact]
    public void GenerateToken_EmbedsExpectedClaimsIssuerAndAudience()
    {
        var now = DateTime.UtcNow;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super_secret_test_key_1234567890",
                ["Jwt:Issuer"] = "mindmap-tests",
                ["Jwt:Audience"] = "mindmap-users",
                ["Jwt:ExpiryInMinutes"] = "60"
            })
            .Build();

        var service = new TokenService(configuration);

        var token = service.GenerateToken(new User
        {
            Id = 42,
            Username = "alice",
            PasswordHash = "hash"
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("mindmap-tests", jwt.Issuer);
        Assert.Contains("mindmap-users", jwt.Audiences);
        Assert.Contains(jwt.Claims, c =>
            (c.Type == ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.NameId)
            && c.Value == "42");
        Assert.Contains(jwt.Claims, c =>
            (c.Type == ClaimTypes.Name || c.Type == JwtRegisteredClaimNames.UniqueName)
            && c.Value == "alice");
        Assert.InRange(jwt.ValidTo, now.AddMinutes(59), now.AddMinutes(61));
    }

    [Fact]
    public void GenerateToken_Throws_WhenJwtKeyIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "mindmap-tests",
                ["Jwt:Audience"] = "mindmap-users",
                ["Jwt:ExpiryInMinutes"] = "60"
            })
            .Build();

        var service = new TokenService(configuration);

        Assert.Throws<InvalidOperationException>(() => service.GenerateToken(new User
        {
            Id = 1,
            Username = "alice",
            PasswordHash = "hash"
        }));
    }

    [Fact]
    public void GenerateToken_UsesDefaultExpiryWhenConfigValueIsMissing()
    {
        var now = DateTime.UtcNow;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super_secret_test_key_1234567890",
                ["Jwt:Issuer"] = "mindmap-tests",
                ["Jwt:Audience"] = "mindmap-users"
            })
            .Build();

        var service = new TokenService(configuration);

        var token = service.GenerateToken(new User
        {
            Id = 1,
            Username = "alice",
            PasswordHash = "hash"
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.InRange(jwt.ValidTo, now.AddMinutes(59), now.AddMinutes(61));
    }
}
