using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;
using KnowledgeMap.Backend.Services;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Services;

public sealed class MapsServiceTests : ServiceTestBase
{
    private static async Task<(ApplicationDbContext context, MapsService service, User owner, User learner, Map map)> CreateMapFixtureAsync(bool grantLearnerAccess)
    {
        var context = CreateContext();
        var owner = new User { Username = $"owner_{Guid.NewGuid():N}", PasswordHash = "hash" };
        var learner = new User { Username = $"learner_{Guid.NewGuid():N}", PasswordHash = "hash" };
        context.Users.AddRange(owner, learner);
        await context.SaveChangesAsync();

        var service = new MapsService(
            new MapsRepository(context),
            new MapLearningAccessResolver(new MapLearningAccessRepository(context)));

        await service.CreateMapAsync(owner.Id, new CreateMapDto
        {
            Title = "Start map",
            Description = "desc",
            Emoji = "M"
        });

        var map = context.Maps.Single();
        if (grantLearnerAccess)
        {
            context.Accesses.Add(new Access { MapId = map.Id, UserId = learner.Id, Role = "observer" });
            await context.SaveChangesAsync();
        }

        return (context, service, owner, learner, map);
    }

    [Fact]
    public async Task CreateMap_ReturnsCreated()
    {
        await using var context = CreateContext();
        var owner = new User { Username = "owner_map", PasswordHash = "hash" };
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var service = new MapsService(
            new MapsRepository(context),
            new MapLearningAccessResolver(new MapLearningAccessRepository(context)));

        var result = await service.CreateMapAsync(owner.Id, new CreateMapDto
        {
            Title = "Start map",
            Description = "desc",
            Emoji = "M"
        });

        Assert.Equal(ServiceResultType.Created, result.Type);
        Assert.Single(context.Nodes.Where(n => n.MapId == context.Maps.Single().Id));
    }

    [Fact]
    public async Task GetMap_NotFound_ReturnsNotFound()
    {
        var (context, service, owner, _, map) = await CreateMapFixtureAsync(false);
        await using (context)
        {
            var result = await service.GetMapAsync(map.Id + 1000, owner.Id);
            Assert.Equal(ServiceResultType.NotFound, result.Type);
        }
    }

    [Fact]
    public async Task GetMap_WithoutAccess_ReturnsForbidden()
    {
        var (context, service, _, learner, map) = await CreateMapFixtureAsync(false);
        await using (context)
        {
            var result = await service.GetMapAsync(map.Id, learner.Id);
            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task GetMap_WithAccess_ReturnsSuccess()
    {
        var (context, service, _, learner, map) = await CreateMapFixtureAsync(true);
        await using (context)
        {
            var result = await service.GetMapAsync(map.Id, learner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task GetMapNodes_WithoutAccess_ReturnsForbidden()
    {
        var (context, service, _, learner, map) = await CreateMapFixtureAsync(false);
        await using (context)
        {
            var result = await service.GetMapNodesAsync(map.Id, learner.Id);
            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task GetMapNodes_WithAccess_ReturnsSuccess()
    {
        var (context, service, _, learner, map) = await CreateMapFixtureAsync(true);
        await using (context)
        {
            var result = await service.GetMapNodesAsync(map.Id, learner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task GetMapEdges_WithoutAccess_ReturnsForbidden()
    {
        var (context, service, _, learner, map) = await CreateMapFixtureAsync(false);
        await using (context)
        {
            var result = await service.GetMapEdgesAsync(map.Id, learner.Id);
            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task GetMapEdges_WithAccess_ReturnsSuccess()
    {
        var (context, service, _, learner, map) = await CreateMapFixtureAsync(true);
        await using (context)
        {
            var result = await service.GetMapEdgesAsync(map.Id, learner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task GetFullMap_WithoutAccess_ReturnsForbidden()
    {
        var (context, service, _, learner, map) = await CreateMapFixtureAsync(false);
        await using (context)
        {
            var result = await service.GetFullMapAsync(map.Id, learner.Id);
            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task GetFullMap_WithAccess_ReturnsSuccess()
    {
        var (context, service, _, learner, map) = await CreateMapFixtureAsync(true);
        await using (context)
        {
            var result = await service.GetFullMapAsync(map.Id, learner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task UpdateMap_NonOwner_ReturnsForbidden()
    {
        var (context, service, _, learner, map) = await CreateMapFixtureAsync(true);
        await using (context)
        {
            var result = await service.UpdateMapAsync(map.Id, learner.Id, new UpdateMapDto
            {
                Title = "x",
                Description = "x",
                Emoji = "x"
            });

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task UpdateMap_Owner_ReturnsSuccess()
    {
        var (context, service, owner, _, map) = await CreateMapFixtureAsync(true);
        await using (context)
        {
            var result = await service.UpdateMapAsync(map.Id, owner.Id, new UpdateMapDto
            {
                Title = "Updated",
                Description = "Updated desc",
                Emoji = "U"
            });

            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.Equal("Updated", context.Maps.Single().Title);
        }
    }

    [Fact]
    public async Task DeleteMap_NonOwner_ReturnsForbidden()
    {
        var (context, service, _, learner, map) = await CreateMapFixtureAsync(true);
        await using (context)
        {
            var result = await service.DeleteMapAsync(map.Id, learner.Id);
            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task DeleteMap_Owner_ReturnsSuccess()
    {
        var (context, service, owner, _, map) = await CreateMapFixtureAsync(true);
        await using (context)
        {
            var result = await service.DeleteMapAsync(map.Id, owner.Id);
            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.Empty(context.Maps);
        }
    }

    [Fact]
    public async Task GetMyMaps_ReturnsSuccessForOwnerAndLearner()
    {
        var (context, service, owner, learner, _) = await CreateMapFixtureAsync(true);
        await using (context)
        {
            var ownerResult = await service.GetMyMapsAsync(owner.Id);
            var learnerResult = await service.GetMyMapsAsync(learner.Id);

            Assert.Equal(ServiceResultType.Success, ownerResult.Type);
            Assert.Equal(ServiceResultType.Success, learnerResult.Type);
        }
    }
}
