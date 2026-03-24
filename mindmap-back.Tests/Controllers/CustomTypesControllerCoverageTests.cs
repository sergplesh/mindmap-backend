using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class CustomTypesControllerCoverageTests
{
    [Fact]
    public async Task GetCustomNodeTypes_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetCustomNodeTypes(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetCustomNodeType_ReturnsProjectedType_WhenUserHasAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var customType = new NodeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "Formula",
            Color = "#123456",
            Icon = "science",
            Shape = "rounded",
            Size = "large",
            IsSystem = false,
            FieldDefinitions =
            [
                new NodeTypeFieldDefinition
                {
                    Id = 200,
                    Name = "Difficulty",
                    FieldType = "select",
                    SortOrder = 0,
                    Options =
                    [
                        new NodeTypeFieldOption { Id = 300, Value = "Easy", SortOrder = 0 },
                        new NodeTypeFieldOption { Id = 301, Value = "Hard", SortOrder = 1 }
                    ]
                }
            ]
        };

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.NodeTypes.Add(customType);
        context.Accesses.Add(new Access
        {
            Id = 400,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.GetCustomNodeType(map.Id, customType.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;
        var customFields = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(payload, "CustomFields"));
        var fieldPayload = Assert.Single(customFields);
        var options = Assert.IsAssignableFrom<IEnumerable<string>>(AnonymousObjectReader.GetObject(fieldPayload, "Options"));

        Assert.Equal(customType.Id, AnonymousObjectReader.Get<int>(payload, "Id"));
        Assert.Equal("Formula", AnonymousObjectReader.Get<string>(payload, "Name"));
        Assert.False(AnonymousObjectReader.Get<bool>(payload, "IsSystem"));
        Assert.Equal(new[] { "Easy", "Hard" }, options.ToArray());
    }

    [Fact]
    public async Task GetCustomNodeType_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.GetCustomNodeType(map.Id, 100);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetCustomNodeType_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetCustomNodeType(999, 100);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetCustomEdgeTypes_ReturnsSystemAndCustomTypes_WhenUserHasAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var customType = new EdgeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "depends_on",
            Style = "dashed",
            Label = "depends on",
            Color = "#123456",
            IsSystem = false
        };

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.EdgeTypes.Add(customType);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.GetCustomEdgeTypes(map.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;
        var system = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(payload, "system"));
        var custom = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(payload, "custom"));
        var customPayload = Assert.Single(custom);

        Assert.NotEmpty(system);
        Assert.Equal(customType.Id, AnonymousObjectReader.Get<int>(customPayload, "Id"));
        Assert.Equal(customType.Label, AnonymousObjectReader.Get<string>(customPayload, "Label"));
    }

    [Fact]
    public async Task GetCustomEdgeTypes_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetCustomEdgeTypes(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetCustomEdgeTypes_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.GetCustomEdgeTypes(map.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetCustomEdgeType_ReturnsProjectedType_WhenUserHasAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var customType = new EdgeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "supports",
            Style = "solid",
            Label = "supports",
            Color = "#654321",
            IsSystem = false
        };

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.EdgeTypes.Add(customType);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.GetCustomEdgeType(map.Id, customType.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;

        Assert.Equal(customType.Id, AnonymousObjectReader.Get<int>(payload, "Id"));
        Assert.Equal("supports", AnonymousObjectReader.Get<string>(payload, "Name"));
        Assert.Equal("solid", AnonymousObjectReader.Get<string>(payload, "Style"));
        Assert.Equal("#654321", AnonymousObjectReader.Get<string>(payload, "Color"));
    }

    [Fact]
    public async Task GetCustomEdgeType_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.GetCustomEdgeType(map.Id, 100);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetCustomEdgeType_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetCustomEdgeType(999, 100);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetCustomEdgeType_ReturnsNotFound_WhenTypeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetCustomEdgeType(map.Id, 999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateCustomNodeType_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateCustomNodeType(999, new CreateCustomNodeTypeDto
        {
            Name = "Missing",
            Color = "#ffffff"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateCustomEdgeType_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateCustomEdgeType(999, new CreateCustomEdgeTypeDto
        {
            Name = "Missing",
            Style = "solid",
            Color = "#ffffff"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateCustomEdgeType_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Accesses.Add(new Access
        {
            Id = 100,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.CreateCustomEdgeType(map.Id, new CreateCustomEdgeTypeDto
        {
            Name = "Denied",
            Style = "dashed",
            Color = "#ffffff"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateCustomNodeType_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var customType = CreateNodeType(100, map.Id, "Original");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.NodeTypes.Add(customType);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.UpdateCustomNodeType(map.Id, customType.Id, new UpdateCustomNodeTypeDto
        {
            Name = "Denied",
            Color = "#ffffff"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateCustomNodeType_ReturnsNotFound_WhenTypeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateCustomNodeType(map.Id, 999, new UpdateCustomNodeTypeDto
        {
            Name = "Missing",
            Color = "#ffffff"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateCustomNodeType_UsesFallbackShapeAndSize_WhenWhitespacePassed()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var customType = CreateNodeType(100, map.Id, "Original");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.NodeTypes.Add(customType);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateCustomNodeType(map.Id, customType.Id, new UpdateCustomNodeTypeDto
        {
            Name = "Updated",
            Color = "#ffffff",
            Icon = "science",
            Shape = "   ",
            Size = "",
            CustomFields = []
        });

        Assert.IsType<OkObjectResult>(result);

        var savedType = await context.NodeTypes.SingleAsync(t => t.Id == customType.Id);
        Assert.Equal("rect", savedType.Shape);
        Assert.Equal("medium", savedType.Size);
    }

    [Fact]
    public async Task UpdateCustomEdgeType_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var customType = CreateEdgeType(100, map.Id, "Old");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.EdgeTypes.Add(customType);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.UpdateCustomEdgeType(map.Id, customType.Id, new UpdateCustomEdgeTypeDto
        {
            Name = "Denied",
            Style = "dashed",
            Color = "#ffffff"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateCustomEdgeType_ReturnsNotFound_WhenTypeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateCustomEdgeType(map.Id, 999, new UpdateCustomEdgeTypeDto
        {
            Name = "Missing",
            Style = "solid",
            Color = "#ffffff"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCustomNodeType_DeletesUnusedTypeAndAssociatedDefinitions()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var customType = CreateNodeType(100, map.Id, "Unused");
        customType.FieldDefinitions.Add(new NodeTypeFieldDefinition
        {
            Id = 200,
            Name = "Difficulty",
            FieldType = "select",
            SortOrder = 0,
            Options =
            [
                new NodeTypeFieldOption { Id = 300, Value = "Easy", SortOrder = 0 }
            ]
        });

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.NodeTypes.Add(customType);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteCustomNodeType(map.Id, customType.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(await context.NodeTypes.Where(t => !t.IsSystem).ToListAsync());
        Assert.Empty(await context.NodeTypeFieldDefinitions.ToListAsync());
        Assert.Empty(await context.NodeTypeFieldOptions.ToListAsync());
    }

    [Fact]
    public async Task DeleteCustomNodeType_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var customType = CreateNodeType(100, map.Id, "Unused");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.NodeTypes.Add(customType);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.DeleteCustomNodeType(map.Id, customType.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteCustomNodeType_ReturnsNotFound_WhenTypeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteCustomNodeType(map.Id, 999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCustomEdgeType_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var customType = CreateEdgeType(100, map.Id, "Unused");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.EdgeTypes.Add(customType);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.DeleteCustomEdgeType(map.Id, customType.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteCustomEdgeType_ReturnsNotFound_WhenTypeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteCustomEdgeType(map.Id, 999);

        Assert.IsType<NotFoundObjectResult>(result);
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

    private static NodeType CreateNodeType(int id, int mapId, string name) => new()
    {
        Id = id,
        MapId = mapId,
        Name = name,
        Color = "#112233",
        Icon = "science",
        Shape = "rounded",
        Size = "large",
        IsSystem = false
    };

    private static EdgeType CreateEdgeType(int id, int mapId, string name) => new()
    {
        Id = id,
        MapId = mapId,
        Name = name,
        Style = "solid",
        Label = name,
        Color = "#445566",
        IsSystem = false
    };
}
