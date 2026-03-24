using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class NodesControllerCoverageTests
{
    [Fact]
    public async Task CreateNode_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateNode(new CreateNodeDto
        {
            MapId = 999,
            Title = "Node"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateNode_ReturnsForbid_WhenUserIsNotOwner()
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

        var controller = new NodesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.CreateNode(new CreateNodeDto
        {
            MapId = map.Id,
            Title = "Node"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreateNode_ReturnsBadRequest_WhenTypeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateNode(new CreateNodeDto
        {
            MapId = map.Id,
            Title = "Node",
            CustomTypeId = 999
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateNode_ReturnsNotFound_WhenNodeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateNode(999, new UpdateNodeDto
        {
            Title = "Updated"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateNode_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.UpdateNode(node.Id, new UpdateNodeDto
        {
            Title = "Updated"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateNode_ReturnsBadRequest_WhenTypeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateNode(node.Id, new UpdateNodeDto
        {
            Title = "Updated",
            CustomTypeId = 999
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateNode_RemovesCustomFieldValues_WhenTypeIsCleared()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var type = new NodeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "Type",
            Color = "#123456",
            IsSystem = false,
            FieldDefinitions =
            [
                new NodeTypeFieldDefinition
                {
                    Id = 200,
                    Name = "Difficulty",
                    FieldType = "text",
                    SortOrder = 0
                }
            ]
        };
        var node = CreateNode(300, map.Id, "Node");
        node.TypeId = type.Id;

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.NodeTypes.Add(type);
        context.Nodes.Add(node);
        context.NodeFieldValues.Add(new NodeFieldValue
        {
            Id = 400,
            NodeId = node.Id,
            NodeTypeFieldDefinitionId = 200,
            Value = "Hard"
        });
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateNode(node.Id, new UpdateNodeDto
        {
            Title = "Updated",
            Description = "Now plain"
        });

        Assert.IsType<OkObjectResult>(result);

        var savedNode = await context.Nodes.SingleAsync();
        Assert.Null(savedNode.TypeId);
        Assert.Empty(await context.NodeFieldValues.ToListAsync());
    }

    [Fact]
    public async Task UpdateNodePosition_UpdatesCoordinates()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateNodePosition(node.Id, new UpdateNodePositionDto
        {
            XPosition = 123,
            YPosition = 456
        });

        Assert.IsType<OkObjectResult>(result);
        var savedNode = await context.Nodes.SingleAsync();
        Assert.Equal(123, savedNode.XPosition);
        Assert.Equal(456, savedNode.YPosition);
    }

    [Fact]
    public async Task UpdateNodePosition_ReturnsNotFound_WhenNodeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateNodePosition(999, new UpdateNodePositionDto());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateNodePosition_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.UpdateNodePosition(node.Id, new UpdateNodePositionDto());

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteNode_RemovesNodeAndConnectedEdges()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");
        var other = CreateNode(101, map.Id, "Other");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.AddRange(node, other);
        context.Edges.AddRange(
            new Edge
            {
                Id = 200,
                SourceNodeId = node.Id,
                TargetNodeId = other.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            },
            new Edge
            {
                Id = 201,
                SourceNodeId = other.Id,
                TargetNodeId = node.Id,
                IsHierarchy = false,
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteNode(node.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(await context.Nodes.ToListAsync());
        Assert.Empty(await context.Edges.ToListAsync());
    }

    [Fact]
    public async Task DeleteNode_ReturnsNotFound_WhenNodeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteNode(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteNode_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.DeleteNode(node.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetNode_ReturnsNotFound_WhenNodeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetNode(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetNode_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.GetNode(node.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetNode_ReturnsForbid_ForHiddenLearnerNode()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var quizNode = CreateNode(101, map.Id, "Quiz");
        var hiddenNode = CreateNode(102, map.Id, "Hidden");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, quizNode, hiddenNode);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        context.Questions.Add(new Question
        {
            Id = 300,
            NodeId = quizNode.Id,
            QuestionText = "Question",
            QuestionType = "single_choice"
        });
        context.Edges.AddRange(
            new Edge
            {
                Id = 400,
                SourceNodeId = root.Id,
                TargetNodeId = quizNode.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            },
            new Edge
            {
                Id = 401,
                SourceNodeId = quizNode.Id,
                TargetNodeId = hiddenNode.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.GetNode(hiddenNode.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetNode_ReturnsOwnerViewWithCustomFieldsAndCorrectAnswers()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var type = new NodeType
        {
            Id = 100,
            MapId = map.Id,
            Name = "Formula",
            Color = "#123456",
            IsSystem = false,
            FieldDefinitions =
            [
                new NodeTypeFieldDefinition
                {
                    Id = 200,
                    Name = "Difficulty",
                    FieldType = "text",
                    SortOrder = 0
                }
            ]
        };
        var node = CreateNode(300, map.Id, "Node");
        node.TypeId = type.Id;
        node.Description = "Secret";

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.NodeTypes.Add(type);
        context.Nodes.Add(node);
        context.NodeFieldValues.Add(new NodeFieldValue
        {
            Id = 400,
            NodeId = node.Id,
            NodeTypeFieldDefinitionId = 200,
            Value = "Hard"
        });
        context.Questions.Add(new Question
        {
            Id = 500,
            NodeId = node.Id,
            QuestionText = "Question?",
            QuestionType = "single_choice",
            AnswerOptions =
            [
                new AnswerOption { Id = 600, OptionText = "Yes", IsCorrect = true }
            ]
        });
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetNode(node.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;
        var customFields = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(AnonymousObjectReader.GetObject(payload, "CustomFields"));
        var questions = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(payload, "Questions"));
        var questionPayload = Assert.Single(questions);
        var answers = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(questionPayload, "AnswerOptions"));
        var answerPayload = Assert.Single(answers);

        Assert.Equal("Secret", AnonymousObjectReader.Get<string>(payload, "Description"));
        Assert.True(AnonymousObjectReader.Get<bool>(payload, "IsUnlocked"));
        Assert.True(AnonymousObjectReader.Get<bool>(payload, "IsVisible"));
        Assert.Equal("Hard", customFields["Difficulty"]);
        Assert.True(AnonymousObjectReader.Get<bool>(answerPayload, "IsCorrect"));
    }

    [Fact]
    public async Task GetNodeTypeInfo_ReturnsNotFound_WhenNodeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetNodeTypeInfo(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetNodeTypeInfo_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.GetNodeTypeInfo(node.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetNodeTypeInfo_ReturnsMessage_WhenTypeIsNotSet()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetNodeTypeInfo(node.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Тип не установлен", AnonymousObjectReader.Get<string>(ok.Value!, "message"));
    }

    [Fact]
    public async Task GetNodeTypeInfo_ReturnsTypeMetadata()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var type = new NodeType
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
        var node = CreateNode(400, map.Id, "Node");
        node.TypeId = type.Id;

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.NodeTypes.Add(type);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new NodesController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetNodeTypeInfo(node.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;
        var customFields = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(payload, "CustomFields"));
        var fieldPayload = Assert.Single(customFields);
        var options = Assert.IsAssignableFrom<IEnumerable<string>>(AnonymousObjectReader.GetObject(fieldPayload, "Options"));

        Assert.Equal(type.Name, AnonymousObjectReader.Get<string>(payload, "Name"));
        Assert.True(AnonymousObjectReader.Get<bool>(payload, "IsCustom"));
        Assert.Equal(new[] { "Easy", "Hard" }, options.ToArray());
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
