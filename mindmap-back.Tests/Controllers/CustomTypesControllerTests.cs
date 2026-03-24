using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class CustomTypesControllerTests
{
    [Fact]
    public async Task GetCustomNodeTypes_ReturnsSystemAndCustomTypesForUserWithAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var customType = new NodeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "Custom Type",
            Color = "#112233",
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
        context.Accesses.Add(new Access
        {
            Id = 50,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        context.NodeTypes.Add(customType);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.GetCustomNodeTypes(map.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var payload = okResult.Value!;

        var system = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(payload, "system"));
        var custom = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(payload, "custom"));

        Assert.NotEmpty(system);
        var customEntry = Assert.Single(custom);
        Assert.Equal(customType.Id, AnonymousObjectReader.Get<int>(customEntry, "id"));
        Assert.Equal(customType.Name, AnonymousObjectReader.Get<string>(customEntry, "name"));
    }

    [Fact]
    public async Task GetCustomNodeTypes_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.GetCustomNodeTypes(map.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetCustomNodeType_ReturnsNotFound_WhenTypeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetCustomNodeType(map.Id, 999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateCustomNodeType_CreatesTypeWithFieldDefinitionsAndFallbackDefaults()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateCustomNodeType(map.Id, new CreateCustomNodeTypeDto
        {
            Name = "Formula",
            Color = "#abcdef",
            Icon = "functions",
            Shape = "",
            Size = "",
            CustomFields =
            [
                new CustomFieldDto
                {
                    Name = "Difficulty",
                    Type = "select",
                    Required = true,
                    Options = ["Easy", "Hard"]
                }
            ]
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var savedType = await context.NodeTypes
            .Include(t => t.FieldDefinitions)
                .ThenInclude(f => f.Options)
            .SingleAsync(t => !t.IsSystem && t.MapId == map.Id);

        Assert.Equal("Formula", savedType.Name);
        Assert.Equal("rect", savedType.Shape);
        Assert.Equal("medium", savedType.Size);
        var field = Assert.Single(savedType.FieldDefinitions);
        Assert.Equal("Difficulty", field.Name);
        Assert.True(field.IsRequired);
        Assert.Equal(new[] { "Easy", "Hard" }, field.Options.OrderBy(o => o.SortOrder).Select(o => o.Value).ToArray());
    }

    [Fact]
    public async Task CreateCustomNodeType_ReturnsForbid_WhenRequesterIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Accesses.Add(new Access
        {
            Id = 99,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.CreateCustomNodeType(map.Id, new CreateCustomNodeTypeDto
        {
            Name = "Nope",
            Color = "#ffffff"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateCustomNodeType_ReplacesDeletedDefinitionsAndRemovesFieldValues()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var nodeType = new NodeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "Original",
            Color = "#112233",
            Icon = "category",
            Shape = "rect",
            Size = "medium",
            IsSystem = false
        };
        var keepDefinition = new NodeTypeFieldDefinition
        {
            Id = 200,
            NodeTypeId = nodeType.Id,
            Name = "Keep",
            FieldType = "text",
            SortOrder = 0
        };
        var removeDefinition = new NodeTypeFieldDefinition
        {
            Id = 201,
            NodeTypeId = nodeType.Id,
            Name = "Remove",
            FieldType = "text",
            SortOrder = 1,
            Options = [new NodeTypeFieldOption { Id = 300, Value = "Old", SortOrder = 0 }]
        };
        nodeType.FieldDefinitions.Add(keepDefinition);
        nodeType.FieldDefinitions.Add(removeDefinition);

        var node = new Node
        {
            Id = 400,
            MapId = map.Id,
            TypeId = nodeType.Id,
            Title = "Node",
            XPosition = 0,
            YPosition = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.NodeTypes.Add(nodeType);
        context.Nodes.Add(node);
        context.NodeFieldValues.Add(new NodeFieldValue
        {
            Id = 500,
            NodeId = node.Id,
            NodeTypeFieldDefinitionId = removeDefinition.Id,
            Value = "legacy"
        });
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateCustomNodeType(map.Id, nodeType.Id, new UpdateCustomNodeTypeDto
        {
            Name = "Updated",
            Color = "#445566",
            Icon = "science",
            Shape = "rounded",
            Size = "large",
            CustomFields =
            [
                new CustomFieldDto
                {
                    Name = "Keep",
                    Type = "number",
                    Required = true,
                    DefaultValue = "5"
                },
                new CustomFieldDto
                {
                    Name = "Added",
                    Type = "select",
                    Options = ["A", "B"]
                }
            ]
        });

        Assert.IsType<OkObjectResult>(result);

        var updatedType = await context.NodeTypes
            .Include(t => t.FieldDefinitions)
                .ThenInclude(f => f.Options)
            .SingleAsync(t => t.Id == nodeType.Id);

        Assert.Equal("Updated", updatedType.Name);
        Assert.Equal("rounded", updatedType.Shape);
        Assert.Equal("large", updatedType.Size);
        Assert.DoesNotContain(updatedType.FieldDefinitions, f => f.Name == "Remove");
        Assert.Empty(context.NodeFieldValues);

        var kept = Assert.Single(updatedType.FieldDefinitions, f => f.Name == "Keep");
        Assert.Equal("number", kept.FieldType);
        Assert.True(kept.IsRequired);
        Assert.Equal("5", kept.DefaultValue);

        var added = Assert.Single(updatedType.FieldDefinitions, f => f.Name == "Added");
        Assert.Equal(new[] { "A", "B" }, added.Options.OrderBy(o => o.SortOrder).Select(o => o.Value).ToArray());
    }

    [Fact]
    public async Task DeleteCustomNodeType_ReturnsBadRequest_WhenTypeIsUsedByNode()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var nodeType = new NodeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "In Use",
            Color = "#112233",
            Shape = "rect",
            Size = "medium",
            IsSystem = false
        };
        var node = new Node
        {
            Id = 400,
            MapId = map.Id,
            TypeId = nodeType.Id,
            Title = "Node",
            XPosition = 0,
            YPosition = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.NodeTypes.Add(nodeType);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteCustomNodeType(map.Id, nodeType.Id);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.True(await context.NodeTypes.AnyAsync(t => t.Id == nodeType.Id));
    }

    [Fact]
    public async Task CreateCustomEdgeType_CreatesEdgeTypeForOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateCustomEdgeType(map.Id, new CreateCustomEdgeTypeDto
        {
            Name = "depends_on",
            Style = "dashed",
            Label = "depends on",
            Color = "#778899"
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var edgeType = await context.EdgeTypes.SingleAsync(t => !t.IsSystem && t.MapId == map.Id);
        Assert.Equal("depends_on", edgeType.Name);
        Assert.Equal("dashed", edgeType.Style);
        Assert.Equal("depends on", edgeType.Label);
        Assert.Equal("#778899", edgeType.Color);
    }

    [Fact]
    public async Task UpdateCustomEdgeType_UpdatesFields()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var edgeType = new EdgeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "old",
            Style = "solid",
            Label = "old label",
            Color = "#111111",
            IsSystem = false
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.EdgeTypes.Add(edgeType);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateCustomEdgeType(map.Id, edgeType.Id, new UpdateCustomEdgeTypeDto
        {
            Name = "new",
            Style = "dashed",
            Label = "new label",
            Color = "#222222"
        });

        Assert.IsType<OkObjectResult>(result);
        var updated = await context.EdgeTypes.SingleAsync(t => t.Id == edgeType.Id);
        Assert.Equal("new", updated.Name);
        Assert.Equal("dashed", updated.Style);
        Assert.Equal("new label", updated.Label);
        Assert.Equal("#222222", updated.Color);
    }

    [Fact]
    public async Task DeleteCustomEdgeType_ReturnsBadRequest_WhenTypeIsUsed()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var type = new EdgeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "used",
            Style = "solid",
            Color = "#111111",
            IsSystem = false
        };
        var source = new Node
        {
            Id = 200,
            MapId = map.Id,
            Title = "Source",
            XPosition = 0,
            YPosition = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var target = new Node
        {
            Id = 201,
            MapId = map.Id,
            Title = "Target",
            XPosition = 0,
            YPosition = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.EdgeTypes.Add(type);
        context.Nodes.AddRange(source, target);
        context.Edges.Add(new Edge
        {
            Id = 300,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            TypeId = type.Id,
            IsHierarchy = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteCustomEdgeType(map.Id, type.Id);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteCustomEdgeType_DeletesUnusedType()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var type = new EdgeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "unused",
            Style = "solid",
            Color = "#111111",
            IsSystem = false
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.EdgeTypes.Add(type);
        await context.SaveChangesAsync();

        var controller = new CustomTypesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteCustomEdgeType(map.Id, type.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(context.EdgeTypes.Where(t => !t.IsSystem));
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
}
