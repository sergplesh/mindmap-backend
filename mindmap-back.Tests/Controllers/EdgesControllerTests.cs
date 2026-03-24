using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class EdgesControllerTests
{
    [Fact]
    public async Task CreateEdge_CreatesEdgeWithTrimmedLabelAndResolvedType()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var source = CreateNode(100, map.Id, "Source");
        var target = CreateNode(101, map.Id, "Target");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.AddRange(source, target);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateEdge(new CreateEdgeDto
        {
            MapId = map.Id,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            TypeId = 1,
            Label = "  relates to  ",
            IsHierarchy = false
        });

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var edge = await context.Edges.SingleAsync();

        Assert.Equal(source.Id, edge.SourceNodeId);
        Assert.Equal(target.Id, edge.TargetNodeId);
        Assert.Equal(1, edge.TypeId);
        Assert.Equal("relates to", edge.Label);
        Assert.False(edge.IsHierarchy);

        Assert.NotNull(createdResult.Value);
        var payload = createdResult.Value!;
        Assert.Equal(edge.Id, AnonymousObjectReader.Get<int>(payload, "Id"));
        Assert.Equal(1, AnonymousObjectReader.Get<int>(payload, "TypeId"));
        Assert.Equal("relates to", AnonymousObjectReader.Get<string>(payload, "Label"));
    }

    [Fact]
    public async Task CreateEdge_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateEdge(new CreateEdgeDto
        {
            MapId = 999,
            SourceNodeId = 1,
            TargetNodeId = 2
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateEdge_ReturnsForbid_WhenRequesterIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var requester = CreateUser(2, "requester");
        var map = CreateMap(10, owner.Id);
        var source = CreateNode(100, map.Id, "Source");
        var target = CreateNode(101, map.Id, "Target");

        context.Users.AddRange(owner, requester);
        context.Maps.Add(map);
        context.Nodes.AddRange(source, target);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(requester.Id, requester.Username);

        var result = await controller.CreateEdge(new CreateEdgeDto
        {
            MapId = map.Id,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id
        });

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(context.Edges);
    }

    [Fact]
    public async Task CreateEdge_ReturnsBadRequest_WhenEdgeAlreadyExists()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var source = CreateNode(100, map.Id, "Source");
        var target = CreateNode(101, map.Id, "Target");
        var existingEdge = new Edge
        {
            Id = 500,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            IsHierarchy = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.AddRange(source, target);
        context.Edges.Add(existingEdge);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateEdge(new CreateEdgeDto
        {
            MapId = map.Id,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            TypeId = 1,
            IsHierarchy = false
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(1, await context.Edges.CountAsync());
    }

    [Fact]
    public async Task CreateEdge_ReturnsBadRequest_WhenNodeBelongsToAnotherMap()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var otherMap = CreateMap(11, owner.Id);
        var source = CreateNode(100, map.Id, "Source");
        var target = CreateNode(101, otherMap.Id, "Target");

        context.Users.Add(owner);
        context.Maps.AddRange(map, otherMap);
        context.Nodes.AddRange(source, target);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateEdge(new CreateEdgeDto
        {
            MapId = map.Id,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateEdge_ReturnsBadRequest_WhenTypeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var source = CreateNode(100, map.Id, "Source");
        var target = CreateNode(101, map.Id, "Target");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.AddRange(source, target);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateEdge(new CreateEdgeDto
        {
            MapId = map.Id,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            CustomTypeId = 999
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateEdge_ChangesCustomTypeAndNormalizesLabel()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var source = CreateNode(100, map.Id, "Source");
        var target = CreateNode(101, map.Id, "Target");
        var customType = new EdgeType
        {
            Id = 200,
            MapId = map.Id,
            Name = "custom",
            Style = "dashed",
            Label = "custom label",
            Color = "#123456",
            IsSystem = false
        };
        var edge = new Edge
        {
            Id = 500,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            IsHierarchy = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.AddRange(source, target);
        context.EdgeTypes.Add(customType);
        context.Edges.Add(edge);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateEdge(edge.Id, new UpdateEdgeDto
        {
            CustomTypeId = customType.Id,
            Label = "  custom relation  "
        });

        Assert.IsType<OkObjectResult>(result);

        var updatedEdge = await context.Edges.SingleAsync();
        Assert.Equal(customType.Id, updatedEdge.TypeId);
        Assert.Equal("custom relation", updatedEdge.Label);
    }

    [Fact]
    public async Task UpdateEdge_ReturnsNotFound_WhenEdgeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateEdge(999, new UpdateEdgeDto { Label = "test" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateEdge_ReturnsBadRequest_WhenTypeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var source = CreateNode(100, map.Id, "Source");
        var target = CreateNode(101, map.Id, "Target");
        var edge = new Edge
        {
            Id = 500,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            IsHierarchy = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.AddRange(source, target);
        context.Edges.Add(edge);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateEdge(edge.Id, new UpdateEdgeDto { CustomTypeId = 999 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateEdge_StoresNullLabel_WhenWhitespacePassed()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var source = CreateNode(100, map.Id, "Source");
        var target = CreateNode(101, map.Id, "Target");
        var edge = new Edge
        {
            Id = 500,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            TypeId = 1,
            Label = "old",
            IsHierarchy = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.AddRange(source, target);
        context.Edges.Add(edge);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateEdge(edge.Id, new UpdateEdgeDto { Label = "   " });

        Assert.IsType<OkObjectResult>(result);
        Assert.Null((await context.Edges.SingleAsync()).Label);
    }

    [Fact]
    public async Task GetEdge_ReturnsForbid_WhenUserHasNoAccessToMap()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var stranger = CreateUser(2, "stranger");
        var map = CreateMap(10, owner.Id);
        var source = CreateNode(100, map.Id, "Source");
        var target = CreateNode(101, map.Id, "Target");
        var edge = new Edge
        {
            Id = 500,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            TypeId = 1,
            IsHierarchy = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(owner, stranger);
        context.Maps.Add(map);
        context.Nodes.AddRange(source, target);
        context.Edges.Add(edge);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(stranger.Id, stranger.Username);

        var result = await controller.GetEdge(edge.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetEdge_ReturnsNotFound_WhenEdgeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetEdge(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetEdge_UsesTypeLabel_WhenCustomLabelMissing()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var source = CreateNode(100, map.Id, "Source");
        var target = CreateNode(101, map.Id, "Target");
        var edge = new Edge
        {
            Id = 500,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            TypeId = 1,
            Label = null,
            IsHierarchy = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.AddRange(source, target);
        context.Edges.Add(edge);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetEdge(edge.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal("является", AnonymousObjectReader.Get<string>(ok.Value!, "Label"));
    }

    [Fact]
    public async Task DeleteEdge_DeletesEdgeForOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var source = CreateNode(100, map.Id, "Source");
        var target = CreateNode(101, map.Id, "Target");
        var edge = new Edge
        {
            Id = 500,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            IsHierarchy = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.AddRange(source, target);
        context.Edges.Add(edge);
        await context.SaveChangesAsync();

        var controller = new EdgesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteEdge(edge.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(context.Edges);
    }

    private static User CreateUser(int id, string username) => new()
    {
        Id = id,
        Username = username,
        PasswordHash = "hash"
    };

    private static Map CreateMap(int id, int ownerId) => new()
    {
        Id = id,
        OwnerId = ownerId,
        Title = "Map",
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
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
