using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;
using KnowledgeMap.Backend.Services;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Services;

public sealed class CustomTypesServiceTests : ServiceTestBase
{
    private static async Task<(ApplicationDbContext context, CustomTypesService service, User owner, User learner, Map map, Node root, Node child)> CreateFixtureAsync()
    {
        var context = CreateContext();
        var (owner, learner, _, map, root, child) = await SeedBasicMapAsync(context);
        var service = new CustomTypesService(new CustomTypesRepository(context));
        return (context, service, owner, learner, map, root, child);
    }

    [Fact]
    public async Task GetCustomNodeTypes_MapNotFound_ReturnsNotFound()
    {
        var (context, service, owner, _, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.GetCustomNodeTypesAsync(map.Id + 999, owner.Id);
            Assert.Equal(ServiceResultType.NotFound, result.Type);
        }
    }

    [Fact]
    public async Task CreateCustomNodeType_NonOwner_ReturnsForbidden()
    {
        var (context, service, _, learner, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateCustomNodeTypeAsync(map.Id, learner.Id, new CreateCustomNodeTypeDto
            {
                Name = "Denied",
                Color = "#111111"
            });

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task CreateCustomNodeType_Owner_ReturnsSuccess()
    {
        var (context, service, owner, _, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateCustomNodeTypeAsync(map.Id, owner.Id, new CreateCustomNodeTypeDto
            {
                Name = "Practice",
                Color = "#112233",
                Icon = " ",
                Shape = " ",
                Size = " ",
                CustomFields = new List<CustomFieldDto>
                {
                    new() { Name = "Difficulty", Type = "number", DefaultValue = "1", Required = true, Options = new List<string>() },
                    new() { Name = "Tags", Type = "select", Options = new List<string> { "A", "B" } }
                }
            });

            Assert.Equal(ServiceResultType.Success, result.Type);

            var type = await context.NodeTypes
                .Include(t => t.FieldDefinitions)
                .ThenInclude(f => f.Options)
                .SingleAsync(t => t.MapId == map.Id && t.Name == "Practice");

            Assert.Null(type.Icon);
            Assert.Equal("rect", type.Shape);
            Assert.Equal("medium", type.Size);
            Assert.Equal(2, type.FieldDefinitions.Count);
        }
    }

    [Fact]
    public async Task UpdateCustomNodeType_NotFound_ReturnsNotFound()
    {
        var (context, service, owner, _, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.UpdateCustomNodeTypeAsync(map.Id, 999999, owner.Id, new UpdateCustomNodeTypeDto
            {
                Name = "x",
                Color = "#000000"
            });

            Assert.Equal(ServiceResultType.NotFound, result.Type);
        }
    }

    [Fact]
    public async Task UpdateCustomNodeType_Owner_ReturnsSuccess()
    {
        var (context, service, owner, _, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateCustomNodeTypeAsync(map.Id, owner.Id, new CreateCustomNodeTypeDto
            {
                Name = "Practice",
                Color = "#112233",
                CustomFields = new List<CustomFieldDto> { new() { Name = "Difficulty", Type = "number" } }
            });
            var type = await context.NodeTypes.SingleAsync(t => t.MapId == map.Id && t.Name == "Practice");

            var result = await service.UpdateCustomNodeTypeAsync(map.Id, type.Id, owner.Id, new UpdateCustomNodeTypeDto
            {
                Name = "PracticeUpdated",
                Color = "#445566",
                Icon = "task",
                Shape = "circle",
                Size = "large",
                CustomFields = new List<CustomFieldDto> { new() { Name = "OnlyOne", Type = "text", Required = false } }
            });

            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.Equal("PracticeUpdated", (await context.NodeTypes.SingleAsync(t => t.Id == type.Id)).Name);
        }
    }

    [Fact]
    public async Task DeleteCustomNodeType_WhenUsed_ReturnsBadRequest()
    {
        var (context, service, owner, _, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateCustomNodeTypeAsync(map.Id, owner.Id, new CreateCustomNodeTypeDto
            {
                Name = "Practice",
                Color = "#112233"
            });
            var type = await context.NodeTypes.SingleAsync(t => t.MapId == map.Id && t.Name == "Practice");

            context.Nodes.Add(new Node
            {
                MapId = map.Id,
                TypeId = type.Id,
                Title = "Typed",
                Description = "d",
                XPosition = 1,
                YPosition = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var result = await service.DeleteCustomNodeTypeAsync(map.Id, type.Id, owner.Id);
            Assert.Equal(ServiceResultType.BadRequest, result.Type);
        }
    }

    [Fact]
    public async Task DeleteCustomNodeType_WhenUnused_ReturnsSuccess()
    {
        var (context, service, owner, _, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateCustomNodeTypeAsync(map.Id, owner.Id, new CreateCustomNodeTypeDto
            {
                Name = "Practice",
                Color = "#112233"
            });
            var type = await context.NodeTypes.SingleAsync(t => t.MapId == map.Id && t.Name == "Practice");

            var result = await service.DeleteCustomNodeTypeAsync(map.Id, type.Id, owner.Id);

            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.DoesNotContain(context.NodeTypes, t => t.Id == type.Id);
        }
    }

    [Fact]
    public async Task GetCustomNodeTypes_AccessUser_ReturnsSuccess()
    {
        var (context, service, _, learner, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.GetCustomNodeTypesAsync(map.Id, learner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task GetCustomNodeTypes_NoAccessUser_ReturnsForbidden()
    {
        var (context, service, _, _, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.GetCustomNodeTypesAsync(map.Id, 999999);
            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task GetCustomEdgeTypes_AccessUser_ReturnsSuccess()
    {
        var (context, service, _, learner, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.GetCustomEdgeTypesAsync(map.Id, learner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task CreateCustomEdgeType_Owner_ReturnsSuccess()
    {
        var (context, service, owner, _, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateCustomEdgeTypeAsync(map.Id, owner.Id, new CreateCustomEdgeTypeDto
            {
                Name = "DependsOn",
                Style = "dashed",
                Label = "depends",
                Color = "#abcdef"
            });

            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task GetCustomEdgeType_AccessUser_ReturnsSuccess()
    {
        var (context, service, owner, learner, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateCustomEdgeTypeAsync(map.Id, owner.Id, new CreateCustomEdgeTypeDto
            {
                Name = "DependsOn",
                Style = "dashed",
                Label = "depends",
                Color = "#abcdef"
            });
            var edgeType = await context.EdgeTypes.SingleAsync(t => t.MapId == map.Id && t.Name == "DependsOn");

            var result = await service.GetCustomEdgeTypeAsync(map.Id, edgeType.Id, learner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task UpdateCustomEdgeType_NonOwner_ReturnsForbidden()
    {
        var (context, service, owner, learner, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateCustomEdgeTypeAsync(map.Id, owner.Id, new CreateCustomEdgeTypeDto
            {
                Name = "DependsOn",
                Style = "dashed",
                Label = "depends",
                Color = "#abcdef"
            });
            var edgeType = await context.EdgeTypes.SingleAsync(t => t.MapId == map.Id && t.Name == "DependsOn");

            var result = await service.UpdateCustomEdgeTypeAsync(map.Id, edgeType.Id, learner.Id, new UpdateCustomEdgeTypeDto
            {
                Name = "x",
                Style = "solid",
                Color = "#000000"
            });

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task UpdateCustomEdgeType_Owner_ReturnsSuccess()
    {
        var (context, service, owner, _, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateCustomEdgeTypeAsync(map.Id, owner.Id, new CreateCustomEdgeTypeDto
            {
                Name = "DependsOn",
                Style = "dashed",
                Label = "depends",
                Color = "#abcdef"
            });
            var edgeType = await context.EdgeTypes.SingleAsync(t => t.MapId == map.Id && t.Name == "DependsOn");

            var result = await service.UpdateCustomEdgeTypeAsync(map.Id, edgeType.Id, owner.Id, new UpdateCustomEdgeTypeDto
            {
                Name = "DependsOnUpdated",
                Style = "solid",
                Label = "updated",
                Color = "#123123"
            });

            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task DeleteCustomEdgeType_WhenUsed_ReturnsBadRequest()
    {
        var (context, service, owner, _, map, root, child) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateCustomEdgeTypeAsync(map.Id, owner.Id, new CreateCustomEdgeTypeDto
            {
                Name = "DependsOn",
                Style = "dashed",
                Label = "depends",
                Color = "#abcdef"
            });
            var edgeType = await context.EdgeTypes.SingleAsync(t => t.MapId == map.Id && t.Name == "DependsOn");

            context.Edges.Add(new Edge
            {
                SourceNodeId = child.Id,
                TargetNodeId = root.Id,
                TypeId = edgeType.Id,
                CreatedAt = DateTime.UtcNow,
                IsHierarchy = false
            });
            await context.SaveChangesAsync();

            var result = await service.DeleteCustomEdgeTypeAsync(map.Id, edgeType.Id, owner.Id);
            Assert.Equal(ServiceResultType.BadRequest, result.Type);
        }
    }

    [Fact]
    public async Task DeleteCustomEdgeType_WhenUnused_ReturnsSuccess()
    {
        var (context, service, owner, _, map, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateCustomEdgeTypeAsync(map.Id, owner.Id, new CreateCustomEdgeTypeDto
            {
                Name = "DependsOn",
                Style = "dashed",
                Label = "depends",
                Color = "#abcdef"
            });
            var edgeType = await context.EdgeTypes.SingleAsync(t => t.MapId == map.Id && t.Name == "DependsOn");

            var result = await service.DeleteCustomEdgeTypeAsync(map.Id, edgeType.Id, owner.Id);

            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.Equal(ServiceResultType.NotFound, (await service.GetCustomEdgeTypeAsync(map.Id, edgeType.Id, owner.Id)).Type);
        }
    }
}
