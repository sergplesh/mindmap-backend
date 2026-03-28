using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;
using KnowledgeMap.Backend.Services;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Services;

public sealed class NodesServiceTests : ServiceTestBase
{
    private static async Task<(ApplicationDbContext context, NodesService service, User owner, User learner, User outsider, Map map, Node root, Node child, NodeType customType)> CreateFixtureAsync(bool withChildQuestion = false)
    {
        var context = CreateContext();
        var (owner, learner, outsider, map, root, child) = await SeedBasicMapAsync(context, withChildQuestion: withChildQuestion);

        var customType = new NodeType
        {
            MapId = map.Id,
            Name = "Task",
            Color = "#123456",
            Icon = "build",
            Shape = "rect",
            Size = "medium",
            IsSystem = false,
            FieldDefinitions = new List<NodeTypeFieldDefinition>
            {
                new() { Name = "Difficulty", FieldType = "number", DefaultValue = "1", SortOrder = 0 },
                new() { Name = "Done", FieldType = "checkbox", DefaultValue = "false", SortOrder = 1 }
            }
        };

        context.NodeTypes.Add(customType);
        await context.SaveChangesAsync();

        var service = new NodesService(
            new NodesRepository(context),
            new MapLearningAccessResolver(new MapLearningAccessRepository(context)));

        return (context, service, owner, learner, outsider, map, root, child, customType);
    }

    [Fact]
    public async Task CreateNode_NonOwner_ReturnsForbidden()
    {
        var (context, service, _, learner, _, map, _, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateNodeAsync(learner.Id, new CreateNodeDto { MapId = map.Id, Title = "Denied" });
            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task CreateNode_InvalidCustomType_ReturnsBadRequest()
    {
        var (context, service, owner, _, _, map, _, _, customType) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateNodeAsync(owner.Id, new CreateNodeDto
            {
                MapId = map.Id,
                Title = "Bad type",
                CustomTypeId = customType.Id + 999
            });

            Assert.Equal(ServiceResultType.BadRequest, result.Type);
        }
    }

    [Fact]
    public async Task CreateNode_WithCustomFields_ReturnsCreated()
    {
        var (context, service, owner, _, _, map, _, _, customType) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateNodeAsync(owner.Id, new CreateNodeDto
            {
                MapId = map.Id,
                Title = "Typed node",
                Description = "d1",
                XPosition = 5,
                YPosition = 6,
                CustomTypeId = customType.Id,
                CustomFields = new Dictionary<string, object>
                {
                    ["Difficulty"] = 7,
                    ["Done"] = true
                }
            });

            Assert.Equal(ServiceResultType.Created, result.Type);

            var typedNodeId = context.Nodes.Single(n => n.Title == "Typed node").Id;
            var values = await context.NodeFieldValues.Include(v => v.FieldDefinition).Where(v => v.NodeId == typedNodeId).ToListAsync();
            Assert.Equal("7", values.Single(v => v.FieldDefinition.Name == "Difficulty").Value);
            Assert.Equal("true", values.Single(v => v.FieldDefinition.Name == "Done").Value);
        }
    }

    [Fact]
    public async Task UpdateNode_UpdatesDefaultsWhenFieldNotProvided()
    {
        var (context, service, owner, _, _, map, _, _, customType) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateNodeAsync(owner.Id, new CreateNodeDto
            {
                MapId = map.Id,
                Title = "Typed node",
                CustomTypeId = customType.Id,
                CustomFields = new Dictionary<string, object> { ["Difficulty"] = 7, ["Done"] = true }
            });
            var typedNode = context.Nodes.Single(n => n.Title == "Typed node");

            var result = await service.UpdateNodeAsync(typedNode.Id, owner.Id, new UpdateNodeDto
            {
                Title = "Typed node updated",
                Description = "d2",
                CustomTypeId = customType.Id,
                CustomFields = new Dictionary<string, object> { ["Done"] = false }
            });

            Assert.Equal(ServiceResultType.Success, result.Type);

            var values = await context.NodeFieldValues.Include(v => v.FieldDefinition).Where(v => v.NodeId == typedNode.Id).ToListAsync();
            Assert.Equal("1", values.Single(v => v.FieldDefinition.Name == "Difficulty").Value);
            Assert.Equal("false", values.Single(v => v.FieldDefinition.Name == "Done").Value);
        }
    }

    [Fact]
    public async Task UpdateNode_ClearType_RemovesFieldValues()
    {
        var (context, service, owner, _, _, map, _, _, customType) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateNodeAsync(owner.Id, new CreateNodeDto
            {
                MapId = map.Id,
                Title = "Typed node",
                CustomTypeId = customType.Id,
                CustomFields = new Dictionary<string, object> { ["Difficulty"] = 7, ["Done"] = true }
            });
            var typedNode = context.Nodes.Single(n => n.Title == "Typed node");

            var result = await service.UpdateNodeAsync(typedNode.Id, owner.Id, new UpdateNodeDto
            {
                Title = "No type",
                Description = "d3",
                TypeId = null,
                CustomTypeId = null
            });

            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.Empty(context.NodeFieldValues.Where(v => v.NodeId == typedNode.Id));
        }
    }

    [Fact]
    public async Task UpdateNodePosition_NonOwner_ReturnsForbidden()
    {
        var (context, service, owner, learner, _, map, _, _, customType) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateNodeAsync(owner.Id, new CreateNodeDto { MapId = map.Id, Title = "Typed node", CustomTypeId = customType.Id });
            var typedNode = context.Nodes.Single(n => n.Title == "Typed node");

            var result = await service.UpdateNodePositionAsync(typedNode.Id, learner.Id, new UpdateNodePositionDto
            {
                XPosition = 20,
                YPosition = 30
            });

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task UpdateNodePosition_Owner_ReturnsSuccess()
    {
        var (context, service, owner, _, _, map, _, _, customType) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateNodeAsync(owner.Id, new CreateNodeDto { MapId = map.Id, Title = "Typed node", CustomTypeId = customType.Id });
            var typedNode = context.Nodes.Single(n => n.Title == "Typed node");

            var result = await service.UpdateNodePositionAsync(typedNode.Id, owner.Id, new UpdateNodePositionDto
            {
                XPosition = 20,
                YPosition = 30
            });

            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task GetNodeTypeInfo_ReturnsSuccessForOwner()
    {
        var (context, service, owner, _, _, map, _, _, customType) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateNodeAsync(owner.Id, new CreateNodeDto { MapId = map.Id, Title = "Typed node", CustomTypeId = customType.Id });
            var typedNode = context.Nodes.Single(n => n.Title == "Typed node");

            var result = await service.GetNodeTypeInfoAsync(typedNode.Id, owner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task DeleteNode_Owner_RemovesNodeAndEdges()
    {
        var (context, service, owner, _, _, _, root, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.DeleteNodeAsync(root.Id, owner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.DoesNotContain(context.Nodes, n => n.Id == root.Id);
            Assert.DoesNotContain(context.Edges, e => e.SourceNodeId == root.Id || e.TargetNodeId == root.Id);
        }
    }

    [Fact]
    public async Task GetNodeAsync_LearnerSeesLockedNodeWithoutContent()
    {
        var (context, service, _, learner, _, _, _, child, _) = await CreateFixtureAsync(withChildQuestion: true);
        await using (context)
        {
            var result = await service.GetNodeAsync(child.Id, learner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);

            var description = result.Value!.GetType().GetProperty("Description")!.GetValue(result.Value);
            var isUnlocked = (bool)result.Value.GetType().GetProperty("IsUnlocked")!.GetValue(result.Value)!;
            Assert.Null(description);
            Assert.False(isUnlocked);
        }
    }

    [Fact]
    public async Task GetNodeAsync_HiddenDescendantForLearner_ReturnsForbidden()
    {
        var (context, service, _, learner, _, map, _, child, _) = await CreateFixtureAsync(withChildQuestion: true);
        await using (context)
        {
            var grandChild = new Node
            {
                MapId = map.Id,
                Title = "GrandChild",
                Description = "Hidden",
                XPosition = 200,
                YPosition = 200,
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

            var result = await service.GetNodeAsync(grandChild.Id, learner.Id);
            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task GetNodeAsync_OutsiderWithoutAccess_ReturnsForbidden()
    {
        var (context, service, _, _, outsider, _, _, child, _) = await CreateFixtureAsync(withChildQuestion: true);
        await using (context)
        {
            var result = await service.GetNodeAsync(child.Id, outsider.Id);
            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task GetNodeAsync_LearnerAfterPassingQuiz_ReturnsSuccessWithContent()
    {
        var (context, service, _, learner, _, _, _, child, _) = await CreateFixtureAsync(withChildQuestion: true);
        await using (context)
        {
            context.AnswerResults.Add(new AnswerResult
            {
                UserId = learner.Id,
                NodeId = child.Id,
                IsPassed = true,
                CompletedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var result = await service.GetNodeAsync(child.Id, learner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);

            var description = result.Value!.GetType().GetProperty("Description")!.GetValue(result.Value);
            var isUnlocked = (bool)result.Value.GetType().GetProperty("IsUnlocked")!.GetValue(result.Value)!;
            Assert.NotNull(description);
            Assert.True(isUnlocked);
        }
    }
}
