using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class MapsControllerCoverageTests
{
    [Fact]
    public async Task GetMap_ReturnsProjectedDataForSharedUser()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id, "Physics");
        var systemType = new NodeType
        {
            Id = 100,
            Name = "Topic",
            Color = "#123456",
            IsSystem = true
        };
        var customEdgeType = new EdgeType
        {
            Id = 200,
            MapId = map.Id,
            Name = "Depends",
            Style = "dashed",
            Label = "depends on",
            Color = "#654321",
            IsSystem = false
        };
        var typedNode = CreateNode(300, map.Id, "Typed");
        typedNode.TypeId = systemType.Id;
        var fallbackNode = CreateNode(301, map.Id, "Untyped");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.NodeTypes.Add(systemType);
        context.EdgeTypes.Add(customEdgeType);
        context.Nodes.AddRange(typedNode, fallbackNode);
        context.Questions.Add(new Question
        {
            Id = 400,
            NodeId = typedNode.Id,
            QuestionText = "Question?",
            QuestionType = "single_choice"
        });
        context.Edges.AddRange(
            new Edge
            {
                Id = 500,
                SourceNodeId = typedNode.Id,
                TargetNodeId = fallbackNode.Id,
                TypeId = customEdgeType.Id,
                IsHierarchy = false,
                CreatedAt = DateTime.UtcNow
            },
            new Edge
            {
                Id = 501,
                SourceNodeId = fallbackNode.Id,
                TargetNodeId = typedNode.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            });
        context.Accesses.Add(new Access
        {
            Id = 600,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.GetMap(map.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;
        var nodes = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(payload, "Nodes"));
        var edges = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(payload, "Edges"));

        Assert.Equal("learner", AnonymousObjectReader.Get<string>(payload, "UserRole"));

        var typedNodePayload = Assert.Single(nodes, node => AnonymousObjectReader.Get<int>(node, "Id") == typedNode.Id);
        var fallbackNodePayload = Assert.Single(nodes, node => AnonymousObjectReader.Get<int>(node, "Id") == fallbackNode.Id);
        var customEdgePayload = Assert.Single(edges, edge => AnonymousObjectReader.Get<int>(edge, "Id") == 500);
        var fallbackEdgePayload = Assert.Single(edges, edge => AnonymousObjectReader.Get<int>(edge, "Id") == 501);

        Assert.Equal(systemType.Id, AnonymousObjectReader.Get<int>(typedNodePayload, "TypeId"));
        Assert.False(AnonymousObjectReader.Get<bool>(typedNodePayload, "IsCustomType"));
        Assert.True(AnonymousObjectReader.Get<bool>(typedNodePayload, "HasQuestions"));

        Assert.Equal("Неизвестно", AnonymousObjectReader.Get<string>(fallbackNodePayload, "TypeName"));
        Assert.Equal("#3b82f6", AnonymousObjectReader.Get<string>(fallbackNodePayload, "TypeColor"));
        Assert.False(AnonymousObjectReader.Get<bool>(fallbackNodePayload, "IsCustomType"));

        Assert.Equal(customEdgeType.Id, AnonymousObjectReader.Get<int>(customEdgePayload, "CustomTypeId"));
        Assert.True(AnonymousObjectReader.Get<bool>(customEdgePayload, "IsCustomType"));
        Assert.Equal("depends on", AnonymousObjectReader.Get<string>(customEdgePayload, "TypeLabel"));

        Assert.Equal("Неизвестно", AnonymousObjectReader.Get<string>(fallbackEdgePayload, "TypeName"));
        Assert.Equal("solid", AnonymousObjectReader.Get<string>(fallbackEdgePayload, "TypeStyle"));
        Assert.Equal(string.Empty, AnonymousObjectReader.Get<string>(fallbackEdgePayload, "TypeLabel"));
        Assert.Equal("#666666", AnonymousObjectReader.Get<string>(fallbackEdgePayload, "TypeColor"));
        Assert.False(AnonymousObjectReader.Get<bool>(fallbackEdgePayload, "IsCustomType"));
        Assert.Null(AnonymousObjectReader.GetObject(fallbackEdgePayload, "CustomTypeId"));
    }

    [Fact]
    public async Task GetMap_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var user = CreateUser(1, "user");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(user.Id, user.Username);

        var result = await controller.GetMap(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMap_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.GetMap(map.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateMap_UpdatesOwnedMap()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id, "Before");

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateMap(map.Id, new UpdateMapDto
        {
            Title = "After",
            Description = "Updated",
            Emoji = "рџ“"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("After", AnonymousObjectReader.Get<string>(ok.Value!, "Title"));

        var savedMap = await context.Maps.SingleAsync();
        Assert.Equal("After", savedMap.Title);
        Assert.Equal("Updated", savedMap.Description);
        Assert.Equal("рџ“", savedMap.Emoji);
    }

    [Fact]
    public async Task UpdateMap_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateMap(999, new UpdateMapDto
        {
            Title = "Updated"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateMap_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id, "Before");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Accesses.Add(new Access
        {
            Id = 300,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.UpdateMap(map.Id, new UpdateMapDto
        {
            Title = "Updated"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteMap_RemovesMapAndEdges()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var child = CreateNode(101, map.Id, "Child");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, child);
        context.Edges.Add(new Edge
        {
            Id = 200,
            SourceNodeId = root.Id,
            TargetNodeId = child.Id,
            IsHierarchy = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteMap(map.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(await context.Maps.ToListAsync());
        Assert.Empty(await context.Edges.ToListAsync());
    }

    [Fact]
    public async Task DeleteMap_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteMap(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMap_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Accesses.Add(new Access
        {
            Id = 300,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.DeleteMap(map.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetMapNodes_ReturnsProjectedNodesWithFallbacks()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var systemType = new NodeType
        {
            Id = 100,
            Name = "Topic",
            Color = "#123456",
            IsSystem = true
        };
        var customType = new NodeType
        {
            Id = 101,
            MapId = map.Id,
            Name = "Formula",
            Color = "#abcdef",
            IsSystem = false
        };
        var systemNode = CreateNode(200, map.Id, "System");
        systemNode.TypeId = systemType.Id;
        var customNode = CreateNode(201, map.Id, "Custom");
        customNode.TypeId = customType.Id;
        var untypedNode = CreateNode(202, map.Id, "Untyped");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.NodeTypes.AddRange(systemType, customType);
        context.Nodes.AddRange(systemNode, customNode, untypedNode);
        context.Questions.Add(new Question
        {
            Id = 300,
            NodeId = customNode.Id,
            QuestionText = "Question",
            QuestionType = "single_choice"
        });
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetMapNodes(map.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var nodes = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        var systemNodePayload = Assert.Single(nodes, node => AnonymousObjectReader.Get<int>(node, "Id") == systemNode.Id);
        var customNodePayload = Assert.Single(nodes, node => AnonymousObjectReader.Get<int>(node, "Id") == customNode.Id);
        var untypedNodePayload = Assert.Single(nodes, node => AnonymousObjectReader.Get<int>(node, "Id") == untypedNode.Id);

        Assert.Equal(systemType.Id, AnonymousObjectReader.Get<int>(systemNodePayload, "TypeId"));
        Assert.Equal(customType.Id, AnonymousObjectReader.Get<int>(customNodePayload, "CustomTypeId"));
        Assert.True(AnonymousObjectReader.Get<bool>(customNodePayload, "IsCustomType"));
        Assert.True(AnonymousObjectReader.Get<bool>(customNodePayload, "HasQuestions"));
        Assert.Equal("Неизвестно", AnonymousObjectReader.Get<string>(untypedNodePayload, "TypeName"));
        Assert.Equal("#3b82f6", AnonymousObjectReader.Get<string>(untypedNodePayload, "TypeColor"));
        Assert.Null(AnonymousObjectReader.GetObject(untypedNodePayload, "CustomTypeId"));
    }

    [Fact]
    public async Task GetMapNodes_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.GetMapNodes(map.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetMapEdges_ReturnsProjectedEdgesWithFallbacks()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var customType = new EdgeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "Depends",
            Style = "dashed",
            Label = "depends on",
            Color = "#999999",
            IsSystem = false
        };
        var root = CreateNode(200, map.Id, "Root");
        var child = CreateNode(201, map.Id, "Child");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.EdgeTypes.Add(customType);
        context.Nodes.AddRange(root, child);
        context.Edges.AddRange(
            new Edge
            {
                Id = 300,
                SourceNodeId = root.Id,
                TargetNodeId = child.Id,
                TypeId = customType.Id,
                IsHierarchy = false,
                CreatedAt = DateTime.UtcNow
            },
            new Edge
            {
                Id = 301,
                SourceNodeId = child.Id,
                TargetNodeId = root.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetMapEdges(map.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var edges = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        var customEdgePayload = Assert.Single(edges, edge => AnonymousObjectReader.Get<int>(edge, "Id") == 300);
        var fallbackEdgePayload = Assert.Single(edges, edge => AnonymousObjectReader.Get<int>(edge, "Id") == 301);

        Assert.Equal(customType.Id, AnonymousObjectReader.Get<int>(customEdgePayload, "CustomTypeId"));
        Assert.True(AnonymousObjectReader.Get<bool>(customEdgePayload, "IsCustomType"));
        Assert.Equal("depends on", AnonymousObjectReader.Get<string>(customEdgePayload, "TypeLabel"));
        Assert.Equal("Root", AnonymousObjectReader.Get<string>(customEdgePayload, "SourceNodeTitle"));
        Assert.Equal("Child", AnonymousObjectReader.Get<string>(customEdgePayload, "TargetNodeTitle"));

        Assert.Equal("Неизвестно", AnonymousObjectReader.Get<string>(fallbackEdgePayload, "TypeName"));
        Assert.Equal("solid", AnonymousObjectReader.Get<string>(fallbackEdgePayload, "TypeStyle"));
        Assert.Equal(string.Empty, AnonymousObjectReader.Get<string>(fallbackEdgePayload, "TypeLabel"));
        Assert.False(AnonymousObjectReader.Get<bool>(fallbackEdgePayload, "IsCustomType"));
        Assert.Null(AnonymousObjectReader.GetObject(fallbackEdgePayload, "CustomTypeId"));
    }

    [Fact]
    public async Task GetMapEdges_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.GetMapEdges(map.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetFullMap_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new MapsController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.GetFullMap(map.Id);

        Assert.IsType<ForbidResult>(result);
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

