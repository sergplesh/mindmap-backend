using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;
using KnowledgeMap.Backend.Services;
using Microsoft.Extensions.Configuration;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Services;

public sealed class UtilityServicesTests : ServiceTestBase
{
    [Fact]
    public async Task MapLearningAccessResolver_LocksAndUnlocksHierarchyForLearner()
    {
        await using var context = CreateContext();
        var (owner, learner, _, map, _, child) = await SeedBasicMapAsync(context, withChildQuestion: true);

        var grandChild = new Node
        {
            MapId = map.Id,
            Title = "Grand",
            XPosition = 2,
            YPosition = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Nodes.Add(grandChild);
        await context.SaveChangesAsync();

        context.Edges.Add(new Edge
        {
            SourceNodeId = child.Id,
            TargetNodeId = grandChild.Id,
            IsHierarchy = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var resolver = new MapLearningAccessResolver(new MapLearningAccessRepository(context));

        var lockedSnapshot = await resolver.BuildAsync(map.Id, learner.Id);
        Assert.Equal("learner", lockedSnapshot.UserRole);
        Assert.False(lockedSnapshot.NodeStates[child.Id].IsUnlocked);
        Assert.False(lockedSnapshot.NodeStates[grandChild.Id].IsVisible);

        context.AnswerResults.Add(new AnswerResult
        {
            UserId = learner.Id,
            NodeId = child.Id,
            IsPassed = true,
            CompletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var unlockedSnapshot = await resolver.BuildAsync(map.Id, learner.Id);
        Assert.True(unlockedSnapshot.NodeStates[child.Id].IsUnlocked);
        Assert.True(unlockedSnapshot.NodeStates[grandChild.Id].IsVisible);

        var ownerSnapshot = await resolver.BuildAsync(map.Id, owner.Id);
        Assert.Equal("owner", ownerSnapshot.UserRole);
        Assert.True(ownerSnapshot.NodeStates[child.Id].CanReadContent);
    }

    [Fact]
    public void TokenService_GeneratesClaimsAndFailsWithoutKey()
    {
        var token = CreateTokenService().GenerateToken(new User
        {
            Id = 7,
            Username = "alice",
            PasswordHash = "hash"
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, c =>
            (c.Type == ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.NameId)
            && c.Value == "7");
        Assert.Contains(jwt.Claims, c =>
            (c.Type == ClaimTypes.Name || c.Type == JwtRegisteredClaimNames.UniqueName)
            && c.Value == "alice");

        var configWithoutKey = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "mindmap-tests",
                ["Jwt:Audience"] = "mindmap-users"
            })
            .Build();

        var serviceWithoutKey = new TokenService(configWithoutKey);
        Assert.Throws<InvalidOperationException>(() => serviceWithoutKey.GenerateToken(new User
        {
            Id = 1,
            Username = "missing",
            PasswordHash = "hash"
        }));
    }

    [Fact]
    public void NodeTypeAndScopeMappers_MapValuesAsExpected()
    {
        var definitions = new[]
        {
            new NodeTypeFieldDefinition
            {
                Name = "Enabled",
                FieldType = "checkbox",
                SortOrder = 0
            },
            new NodeTypeFieldDefinition
            {
                Name = "Score",
                FieldType = "number",
                SortOrder = 1
            }
        };

        var dtoResult = NodeTypeFieldMapper.ToDtos(definitions);
        Assert.Equal(2, dtoResult.Count);

        var values = new[]
        {
            new NodeFieldValue
            {
                Value = "true",
                FieldDefinition = definitions[0]
            },
            new NodeFieldValue
            {
                Value = "12.5",
                FieldDefinition = definitions[1]
            }
        };

        var valueDict = NodeTypeFieldMapper.ToValueDictionary(values);
        Assert.NotNull(valueDict);
        Assert.Equal(true, valueDict!["Enabled"]);
        Assert.Equal(12.5d, valueDict["Score"]);

        using var json = JsonDocument.Parse("""{"v":false}""");
        Assert.Equal("false", NodeTypeFieldMapper.ToStorageString(json.RootElement.GetProperty("v")));

        var systemNodeType = new NodeType { Id = 1, IsSystem = true };
        var customEdgeType = new EdgeType { Id = 5, IsSystem = false };

        Assert.Equal(1, TypeScopeMapper.GetSystemNodeTypeId(systemNodeType));
        Assert.Equal(5, TypeScopeMapper.GetCustomEdgeTypeId(customEdgeType));
        Assert.Null(TypeScopeMapper.GetCustomNodeTypeId(systemNodeType));
        Assert.Null(TypeScopeMapper.GetSystemEdgeTypeId(customEdgeType));
    }
}
