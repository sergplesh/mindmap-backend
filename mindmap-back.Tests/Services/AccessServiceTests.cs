using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Repositories;
using KnowledgeMap.Backend.Services;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Services;

public sealed class AccessServiceTests : ServiceTestBase
{
    [Fact]
    public async Task InviteUser_MapNotFound_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var (owner, learner, _, map, _, _) = await SeedBasicMapAsync(context, grantLearnerAccess: false);
        var service = new AccessService(new AccessRepository(context));

        var result = await service.InviteUserAsync(owner.Id, new InviteDto
        {
            MapId = map.Id + 999,
            Username = learner.Username,
            Role = "learner"
        });

        Assert.Equal(ServiceResultType.NotFound, result.Type);
    }

    [Fact]
    public async Task InviteUser_NonOwner_ReturnsForbidden()
    {
        await using var context = CreateContext();
        var (_, learner, outsider, map, _, _) = await SeedBasicMapAsync(context, grantLearnerAccess: false);
        var service = new AccessService(new AccessRepository(context));

        var result = await service.InviteUserAsync(learner.Id, new InviteDto
        {
            MapId = map.Id,
            Username = outsider.Username,
            Role = "observer"
        });

        Assert.Equal(ServiceResultType.Forbidden, result.Type);
    }

    [Fact]
    public async Task InviteUser_SelfInvite_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        var (owner, _, _, map, _, _) = await SeedBasicMapAsync(context, grantLearnerAccess: false);
        var service = new AccessService(new AccessRepository(context));

        var result = await service.InviteUserAsync(owner.Id, new InviteDto
        {
            MapId = map.Id,
            Username = owner.Username,
            Role = "observer"
        });

        Assert.Equal(ServiceResultType.BadRequest, result.Type);
    }

    [Fact]
    public async Task InviteUser_ValidInvite_ReturnsSuccess()
    {
        await using var context = CreateContext();
        var (owner, learner, _, map, _, _) = await SeedBasicMapAsync(context, grantLearnerAccess: false);
        var service = new AccessService(new AccessRepository(context));

        var result = await service.InviteUserAsync(owner.Id, new InviteDto
        {
            MapId = map.Id,
            Username = learner.Username,
            Role = "learner"
        });

        Assert.Equal(ServiceResultType.Success, result.Type);
        Assert.Contains(context.Accesses, a => a.MapId == map.Id && a.UserId == learner.Id);
    }

    [Fact]
    public async Task InviteUser_DuplicateInvite_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        var (owner, learner, _, map, _, _) = await SeedBasicMapAsync(context, grantLearnerAccess: false);
        var service = new AccessService(new AccessRepository(context));

        await service.InviteUserAsync(owner.Id, new InviteDto
        {
            MapId = map.Id,
            Username = learner.Username,
            Role = "learner"
        });

        var duplicate = await service.InviteUserAsync(owner.Id, new InviteDto
        {
            MapId = map.Id,
            Username = learner.Username,
            Role = "learner"
        });

        Assert.Equal(ServiceResultType.BadRequest, duplicate.Type);
    }

    [Fact]
    public async Task GetMapAccess_NonOwner_ReturnsForbidden()
    {
        await using var context = CreateContext();
        var (_, learner, _, map, _, _) = await SeedBasicMapAsync(context, grantLearnerAccess: false);
        var service = new AccessService(new AccessRepository(context));

        var result = await service.GetMapAccessAsync(learner.Id, map.Id);

        Assert.Equal(ServiceResultType.Forbidden, result.Type);
    }

    [Fact]
    public async Task GetMapAccess_Owner_ReturnsSuccess()
    {
        await using var context = CreateContext();
        var (owner, learner, _, map, _, _) = await SeedBasicMapAsync(context, grantLearnerAccess: false);
        var service = new AccessService(new AccessRepository(context));
        await service.InviteUserAsync(owner.Id, new InviteDto
        {
            MapId = map.Id,
            Username = learner.Username,
            Role = "learner"
        });

        var result = await service.GetMapAccessAsync(owner.Id, map.Id);

        Assert.Equal(ServiceResultType.Success, result.Type);
    }

    [Fact]
    public async Task UpdateRole_NonOwner_ReturnsForbidden()
    {
        await using var context = CreateContext();
        var (owner, learner, _, map, _, _) = await SeedBasicMapAsync(context, grantLearnerAccess: false);
        var service = new AccessService(new AccessRepository(context));
        await service.InviteUserAsync(owner.Id, new InviteDto
        {
            MapId = map.Id,
            Username = learner.Username,
            Role = "learner"
        });
        var accessId = context.Accesses.Single(a => a.MapId == map.Id && a.UserId == learner.Id).Id;

        var result = await service.UpdateRoleAsync(learner.Id, accessId, new UpdateRoleDto { Role = "observer" });

        Assert.Equal(ServiceResultType.Forbidden, result.Type);
    }

    [Fact]
    public async Task UpdateRole_Owner_ReturnsSuccess()
    {
        await using var context = CreateContext();
        var (owner, learner, _, map, _, _) = await SeedBasicMapAsync(context, grantLearnerAccess: false);
        var service = new AccessService(new AccessRepository(context));
        await service.InviteUserAsync(owner.Id, new InviteDto
        {
            MapId = map.Id,
            Username = learner.Username,
            Role = "learner"
        });
        var accessId = context.Accesses.Single(a => a.MapId == map.Id && a.UserId == learner.Id).Id;

        var result = await service.UpdateRoleAsync(owner.Id, accessId, new UpdateRoleDto { Role = "observer" });

        Assert.Equal(ServiceResultType.Success, result.Type);
        Assert.Equal("observer", context.Accesses.Single(a => a.Id == accessId).Role);
    }

    [Fact]
    public async Task RemoveAccess_NotFound_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var (owner, _, _, _, _, _) = await SeedBasicMapAsync(context, grantLearnerAccess: false);
        var service = new AccessService(new AccessRepository(context));

        var result = await service.RemoveAccessAsync(owner.Id, 999999);

        Assert.Equal(ServiceResultType.NotFound, result.Type);
    }

    [Fact]
    public async Task RemoveAccess_Owner_ReturnsSuccess()
    {
        await using var context = CreateContext();
        var (owner, learner, _, map, _, _) = await SeedBasicMapAsync(context, grantLearnerAccess: false);
        var service = new AccessService(new AccessRepository(context));
        await service.InviteUserAsync(owner.Id, new InviteDto
        {
            MapId = map.Id,
            Username = learner.Username,
            Role = "learner"
        });
        var accessId = context.Accesses.Single(a => a.MapId == map.Id && a.UserId == learner.Id).Id;

        var result = await service.RemoveAccessAsync(owner.Id, accessId);

        Assert.Equal(ServiceResultType.Success, result.Type);
        Assert.DoesNotContain(context.Accesses, a => a.Id == accessId);
    }
}
