using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class NodesControllerTests
{
    [Fact]
    public async Task CreateNode_CreatesNodeAndSynchronizesCustomFieldValues()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var customType = new NodeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "Concept",
            Color = "#112233",
            Shape = "rect",
            Size = "medium",
            IsSystem = false,
            FieldDefinitions =
            [
                new NodeTypeFieldDefinition
                {
                    Id = 200,
                    Name = "Difficulty",
                    FieldType = "text",
                    SortOrder = 0,
                    DefaultValue = "Medium"
                },
                new NodeTypeFieldDefinition
                {
                    Id = 201,
                    Name = "Score",
                    FieldType = "number",
                    SortOrder = 1,
                    DefaultValue = "10"
                }
            ]
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.NodeTypes.Add(customType);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateNode(new CreateNodeDto
        {
            MapId = map.Id,
            CustomTypeId = customType.Id,
            Title = "New node",
            Description = "Node description",
            XPosition = 11,
            YPosition = 22,
            Width = 300,
            Height = 120,
            CustomFields = new Dictionary<string, object>
            {
                ["Difficulty"] = "Hard"
            }
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var node = await context.Nodes.SingleAsync();
        var fieldValues = await context.NodeFieldValues
            .Include(v => v.FieldDefinition)
            .OrderBy(v => v.FieldDefinition.SortOrder)
            .ToListAsync();

        Assert.Equal(customType.Id, node.TypeId);
        Assert.Equal("New node", node.Title);
        Assert.Equal(11, node.XPosition);
        Assert.Equal(22, node.YPosition);
        Assert.Equal(300, node.Width);
        Assert.Equal(120, node.Height);
        Assert.Equal(2, fieldValues.Count);
        Assert.Equal("Hard", fieldValues[0].Value);
        Assert.Equal("10", fieldValues[1].Value);
        Assert.NotNull(created.Value);
    }

    [Fact]
    public async Task UpdateNode_ChangesTypeAndReplacesOldCustomFieldValues()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var oldType = new NodeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "Old",
            Color = "#111111",
            Shape = "rect",
            Size = "medium",
            IsSystem = false,
            FieldDefinitions =
            [
                new NodeTypeFieldDefinition
                {
                    Id = 200,
                    Name = "Legacy",
                    FieldType = "text",
                    SortOrder = 0
                }
            ]
        };
        var newType = new NodeType
        {
            Id = 101,
            MapId = map.Id,
            Name = "New",
            Color = "#222222",
            Shape = "rounded",
            Size = "large",
            IsSystem = false,
            FieldDefinitions =
            [
                new NodeTypeFieldDefinition
                {
                    Id = 201,
                    Name = "Status",
                    FieldType = "text",
                    SortOrder = 0
                }
            ]
        };
        var node = new Node
        {
            Id = 400,
            MapId = map.Id,
            TypeId = oldType.Id,
            Title = "Node",
            Description = "Before",
            XPosition = 1,
            YPosition = 2,
            Width = 200,
            Height = 80,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.NodeTypes.AddRange(oldType, newType);
        context.Nodes.Add(node);
        context.NodeFieldValues.Add(new NodeFieldValue
        {
            Id = 500,
            NodeId = node.Id,
            NodeTypeFieldDefinitionId = 200,
            Value = "legacy"
        });
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateNode(node.Id, new UpdateNodeDto
        {
            Title = "Updated node",
            Description = "After",
            CustomTypeId = newType.Id,
            XPosition = 10,
            YPosition = 20,
            Width = 350,
            Height = 150,
            CustomFields = new Dictionary<string, object>
            {
                ["Status"] = "Done"
            }
        });

        Assert.IsType<OkObjectResult>(result);

        var updatedNode = await context.Nodes.SingleAsync();
        var fieldValues = await context.NodeFieldValues
            .Include(v => v.FieldDefinition)
            .ToListAsync();

        Assert.Equal(newType.Id, updatedNode.TypeId);
        Assert.Equal("Updated node", updatedNode.Title);
        Assert.Equal("After", updatedNode.Description);
        Assert.Equal(10, updatedNode.XPosition);
        Assert.Equal(20, updatedNode.YPosition);
        Assert.Equal(350, updatedNode.Width);
        Assert.Equal(150, updatedNode.Height);
        var fieldValue = Assert.Single(fieldValues);
        Assert.Equal("Status", fieldValue.FieldDefinition.Name);
        Assert.Equal("Done", fieldValue.Value);
    }

    [Fact]
    public async Task CreateNode_UpdatesMapUpdatedAt()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var oldTimestamp = DateTime.UtcNow.AddMinutes(-10);
        var map = CreateMap(10, owner.Id);
        map.UpdatedAt = oldTimestamp;

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateNode(new CreateNodeDto
        {
            MapId = map.Id,
            Title = "Created node"
        });

        Assert.IsType<CreatedAtActionResult>(result);
        var updatedMap = await context.Maps.SingleAsync(m => m.Id == map.Id);
        Assert.True(updatedMap.UpdatedAt > oldTimestamp);
    }

    [Fact]
    public async Task UpdateNodePosition_UpdatesMapUpdatedAt()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var oldTimestamp = DateTime.UtcNow.AddMinutes(-10);
        var map = CreateMap(10, owner.Id);
        map.UpdatedAt = oldTimestamp;
        var node = CreateNode(100, map.Id, "Node");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateNodePosition(node.Id, new UpdateNodePositionDto
        {
            XPosition = 12,
            YPosition = 34
        });

        Assert.IsType<OkObjectResult>(result);
        var updatedMap = await context.Maps.SingleAsync(m => m.Id == map.Id);
        Assert.True(updatedMap.UpdatedAt > oldTimestamp);
    }

    [Fact]
    public async Task DeleteNode_UpdatesMapUpdatedAt()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var oldTimestamp = DateTime.UtcNow.AddMinutes(-10);
        var map = CreateMap(10, owner.Id);
        map.UpdatedAt = oldTimestamp;
        var node = CreateNode(100, map.Id, "Node");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteNode(node.Id);

        Assert.IsType<OkObjectResult>(result);
        var updatedMap = await context.Maps.SingleAsync(m => m.Id == map.Id);
        Assert.True(updatedMap.UpdatedAt > oldTimestamp);
    }

    [Fact]
    public async Task GetNode_ReturnsVisibleButLockedNodeForLearner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var child = CreateNode(101, map.Id, "Child");
        child.Description = "Secret";

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, child);
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
        context.Edges.Add(new Edge
        {
            Id = 900,
            SourceNodeId = root.Id,
            TargetNodeId = child.Id,
            IsHierarchy = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.GetNode(child.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;

        Assert.Equal(child.Id, AnonymousObjectReader.Get<int>(payload, "Id"));
        Assert.Equal("Child", AnonymousObjectReader.Get<string>(payload, "Title"));
        Assert.Null(AnonymousObjectReader.GetObject(payload, "Description"));
        Assert.True(AnonymousObjectReader.Get<bool>(payload, "HasQuestions"));
        Assert.False(AnonymousObjectReader.Get<bool>(payload, "IsUnlocked"));
        Assert.False(AnonymousObjectReader.Get<bool>(payload, "IsVisible"));
        Assert.Null(AnonymousObjectReader.GetObject(payload, "Questions"));
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
        Width = 200,
        Height = 80,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
