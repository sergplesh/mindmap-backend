using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class MapsControllerTests
{
    [Fact]
    public async Task CreateMap_CreatesCentralNodeWithoutDefaultEmoji()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateMap(new CreateMapDto
        {
            Title = "Physics",
            Description = "Study map"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(created.Value);
        var payload = created.Value!;

        var map = await context.Maps.SingleAsync();
        var rootNode = await context.Nodes.SingleAsync();

        Assert.Equal("Physics", map.Title);
        Assert.Null(map.Emoji);
        Assert.Equal(map.Id, rootNode.MapId);
        Assert.Equal("Physics", rootNode.Title);
        Assert.False(rootNode.RequiresQuiz);
        Assert.Null(AnonymousObjectReader.GetObject(payload, "Emoji"));
    }

    [Fact]
    public async Task GetMyMaps_ReturnsOwnedAndSharedMapsWithRolesAndCounts()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var currentUser = CreateUser(1, "current");
        var otherOwner = CreateUser(2, "other");
        var ownedMap = CreateMap(10, currentUser.Id, "Owned");
        var sharedMap = CreateMap(11, otherOwner.Id, "Shared");
        var ownedRoot = CreateNode(100, ownedMap.Id, "Owned root");
        var ownedChild = CreateNode(101, ownedMap.Id, "Owned child");
        var sharedRoot = CreateNode(102, sharedMap.Id, "Shared root");

        context.Users.AddRange(currentUser, otherOwner);
        context.Maps.AddRange(ownedMap, sharedMap);
        context.Nodes.AddRange(ownedRoot, ownedChild, sharedRoot);
        context.Edges.Add(new Edge
        {
            Id = 500,
            SourceNodeId = ownedRoot.Id,
            TargetNodeId = ownedChild.Id,
            IsHierarchy = true,
            CreatedAt = DateTime.UtcNow
        });
        context.Accesses.Add(new Access
        {
            Id = 600,
            MapId = sharedMap.Id,
            UserId = currentUser.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(currentUser.Id, currentUser.Username);

        var result = await controller.GetMyMaps();

        var ok = Assert.IsType<OkObjectResult>(result);
        var maps = Assert.IsAssignableFrom<IEnumerable<MapDto>>(ok.Value);

        var owned = Assert.Single(maps, m => m.Id == ownedMap.Id);
        var shared = Assert.Single(maps, m => m.Id == sharedMap.Id);

        Assert.Equal("owner", owned.UserRole);
        Assert.Equal(2, owned.NodesCount);
        Assert.Equal(1, owned.EdgesCount);
        Assert.Equal("learner", shared.UserRole);
        Assert.Equal(1, shared.NodesCount);
        Assert.Equal(0, shared.EdgesCount);
    }

    [Fact]
    public async Task GetFullMap_HidesLockedNodeContentForLearner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id, "Map");
        var root = CreateNode(100, map.Id, "Root");
        root.Description = "Visible";
        var child = CreateNode(101, map.Id, "Child");
        child.Description = "Locked";
        var grandChild = CreateNode(102, map.Id, "GrandChild");
        grandChild.Description = "Hidden";

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, child, grandChild);
        context.Accesses.Add(new Access
        {
            Id = 700,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        context.Questions.Add(new Question
        {
            Id = 800,
            NodeId = child.Id,
            QuestionText = "Question?",
            QuestionType = "single_choice"
        });
        context.Edges.AddRange(
            new Edge
            {
                Id = 900,
                SourceNodeId = root.Id,
                TargetNodeId = child.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            },
            new Edge
            {
                Id = 901,
                SourceNodeId = child.Id,
                TargetNodeId = grandChild.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.GetFullMap(map.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;

        var nodes = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(payload, "Nodes"));
        var edges = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(payload, "Edges"));

        var childNode = Assert.Single(nodes, n => AnonymousObjectReader.Get<int>(n, "Id") == child.Id);
        var hiddenNode = Assert.Single(nodes, n => AnonymousObjectReader.Get<int>(n, "Id") == grandChild.Id);
        var visibleEdge = Assert.Single(edges, e => AnonymousObjectReader.Get<int>(e, "Id") == 900);
        var hiddenEdge = Assert.Single(edges, e => AnonymousObjectReader.Get<int>(e, "Id") == 901);

        Assert.Equal("Child", AnonymousObjectReader.Get<string>(childNode, "Title"));
        Assert.Null(AnonymousObjectReader.GetObject(childNode, "Description"));
        Assert.False(AnonymousObjectReader.Get<bool>(childNode, "IsUnlocked"));
        Assert.True(AnonymousObjectReader.Get<bool>(childNode, "IsVisible"));

        Assert.Null(AnonymousObjectReader.GetObject(hiddenNode, "Title"));
        Assert.Null(AnonymousObjectReader.GetObject(hiddenNode, "Description"));
        Assert.False(AnonymousObjectReader.Get<bool>(hiddenNode, "IsVisible"));

        Assert.True(AnonymousObjectReader.Get<bool>(visibleEdge, "IsVisible"));
        Assert.False(AnonymousObjectReader.Get<bool>(hiddenEdge, "IsVisible"));
    }

    private static User CreateUser(int id, string username) => new()
    {
        Id = id,
        Username = username,
        PasswordHash = "hash"
    };

    private static Map CreateMap(int id, int ownerId, string title = "Map") => new()
    {
        Id = id,
        OwnerId = ownerId,
        Title = title,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Node CreateNode(int id, int mapId, string title) => new()
    {
        Id = id,
        MapId = mapId,
        Title = title,
        XPosition = 0,
        YPosition = 0,
        Width = 200,
        Height = 80,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}

