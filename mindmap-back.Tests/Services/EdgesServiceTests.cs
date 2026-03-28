using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;
using KnowledgeMap.Backend.Services;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Services;

public sealed class EdgesServiceTests : ServiceTestBase
{
    private static async Task<(ApplicationDbContext context, EdgesService service, User owner, User learner, User outsider, Map map, Node root, Node child, Node foreignNode)> CreateFixtureAsync()
    {
        var context = CreateContext();
        var (owner, learner, outsider, map, root, child) = await SeedBasicMapAsync(context, grantLearnerAccess: false);

        var secondMap = new Map
        {
            OwnerId = owner.Id,
            Title = "Second",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Maps.Add(secondMap);
        await context.SaveChangesAsync();

        var foreignNode = new Node
        {
            MapId = secondMap.Id,
            Title = "Foreign",
            XPosition = 1,
            YPosition = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Nodes.Add(foreignNode);
        await context.SaveChangesAsync();

        return (context, new EdgesService(new EdgesRepository(context)), owner, learner, outsider, map, root, child, foreignNode);
    }

    [Fact]
    public async Task CreateEdge_NonOwner_ReturnsForbidden()
    {
        var (context, service, _, learner, _, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateEdgeAsync(learner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = root.Id,
                TargetNodeId = child.Id
            });

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task CreateEdge_MissingNode_ReturnsBadRequest()
    {
        var (context, service, owner, _, _, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = root.Id + 1000,
                TargetNodeId = child.Id
            });

            Assert.Equal(ServiceResultType.BadRequest, result.Type);
        }
    }

    [Fact]
    public async Task CreateEdge_NodeFromOtherMap_ReturnsBadRequest()
    {
        var (context, service, owner, _, _, map, root, _, foreignNode) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = root.Id,
                TargetNodeId = foreignNode.Id
            });

            Assert.Equal(ServiceResultType.BadRequest, result.Type);
        }
    }

    [Fact]
    public async Task CreateEdge_DuplicateEdge_ReturnsBadRequest()
    {
        var (context, service, owner, _, _, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = root.Id,
                TargetNodeId = child.Id
            });

            Assert.Equal(ServiceResultType.BadRequest, result.Type);
        }
    }

    [Fact]
    public async Task CreateEdge_InvalidType_ReturnsBadRequest()
    {
        var (context, service, owner, _, _, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = child.Id,
                TargetNodeId = root.Id,
                TypeId = 99999
            });

            Assert.Equal(ServiceResultType.BadRequest, result.Type);
        }
    }

    [Fact]
    public async Task CreateEdge_ValidRequest_ReturnsCreated()
    {
        var (context, service, owner, _, _, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = child.Id,
                TargetNodeId = root.Id,
                TypeId = 1,
                Label = "  edge label  ",
                IsHierarchy = false
            });

            Assert.Equal(ServiceResultType.Created, result.Type);
            Assert.Equal("edge label", context.Edges.Single(e => e.SourceNodeId == child.Id && e.TargetNodeId == root.Id).Label);
        }
    }

    [Fact]
    public async Task GetEdge_NoAccess_ReturnsForbidden()
    {
        var (context, service, owner, _, outsider, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = child.Id,
                TargetNodeId = root.Id
            });
            var newEdge = context.Edges.Single(e => e.SourceNodeId == child.Id && e.TargetNodeId == root.Id);

            var result = await service.GetEdgeAsync(newEdge.Id, outsider.Id);

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task GetEdge_WithAccess_ReturnsSuccess()
    {
        var (context, service, owner, _, outsider, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = child.Id,
                TargetNodeId = root.Id
            });
            var newEdge = context.Edges.Single(e => e.SourceNodeId == child.Id && e.TargetNodeId == root.Id);
            context.Accesses.Add(new Access { MapId = map.Id, UserId = outsider.Id, Role = "observer" });
            await context.SaveChangesAsync();

            var result = await service.GetEdgeAsync(newEdge.Id, outsider.Id);

            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task UpdateEdge_NonOwner_ReturnsForbidden()
    {
        var (context, service, owner, _, outsider, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = child.Id,
                TargetNodeId = root.Id
            });
            var newEdge = context.Edges.Single(e => e.SourceNodeId == child.Id && e.TargetNodeId == root.Id);
            context.Accesses.Add(new Access { MapId = map.Id, UserId = outsider.Id, Role = "observer" });
            await context.SaveChangesAsync();

            var result = await service.UpdateEdgeAsync(newEdge.Id, outsider.Id, new UpdateEdgeDto { Label = "x" });

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task UpdateEdge_InvalidType_ReturnsBadRequest()
    {
        var (context, service, owner, _, _, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = child.Id,
                TargetNodeId = root.Id
            });
            var newEdge = context.Edges.Single(e => e.SourceNodeId == child.Id && e.TargetNodeId == root.Id);

            var result = await service.UpdateEdgeAsync(newEdge.Id, owner.Id, new UpdateEdgeDto { TypeId = 99999 });

            Assert.Equal(ServiceResultType.BadRequest, result.Type);
        }
    }

    [Fact]
    public async Task UpdateEdge_Owner_ReturnsSuccess()
    {
        var (context, service, owner, _, _, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = child.Id,
                TargetNodeId = root.Id
            });
            var newEdge = context.Edges.Single(e => e.SourceNodeId == child.Id && e.TargetNodeId == root.Id);

            var result = await service.UpdateEdgeAsync(newEdge.Id, owner.Id, new UpdateEdgeDto { Label = "  changed  " });

            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.Equal("changed", context.Edges.Single(e => e.Id == newEdge.Id).Label);
        }
    }

    [Fact]
    public async Task DeleteEdge_NonOwner_ReturnsForbidden()
    {
        var (context, service, owner, _, outsider, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = child.Id,
                TargetNodeId = root.Id
            });
            var newEdge = context.Edges.Single(e => e.SourceNodeId == child.Id && e.TargetNodeId == root.Id);
            context.Accesses.Add(new Access { MapId = map.Id, UserId = outsider.Id, Role = "observer" });
            await context.SaveChangesAsync();

            var result = await service.DeleteEdgeAsync(newEdge.Id, outsider.Id);

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task DeleteEdge_Owner_ReturnsSuccess()
    {
        var (context, service, owner, _, _, map, root, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateEdgeAsync(owner.Id, new CreateEdgeDto
            {
                MapId = map.Id,
                SourceNodeId = child.Id,
                TargetNodeId = root.Id
            });
            var newEdge = context.Edges.Single(e => e.SourceNodeId == child.Id && e.TargetNodeId == root.Id);

            var result = await service.DeleteEdgeAsync(newEdge.Id, owner.Id);

            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.Equal(ServiceResultType.NotFound, (await service.GetEdgeAsync(newEdge.Id, owner.Id)).Type);
        }
    }
}
