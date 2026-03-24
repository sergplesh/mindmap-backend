using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class AccessControllerTests
{
    [Fact]
    public async Task InviteUser_CreatesAccessAndReturnsFlattenedPayload()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var invitedUser = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, invitedUser);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.InviteUser(new InviteDto
        {
            MapId = map.Id,
            Username = invitedUser.Username,
            Role = "learner"
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var access = await context.Accesses.SingleAsync();

        Assert.Equal(map.Id, access.MapId);
        Assert.Equal(invitedUser.Id, access.UserId);
        Assert.Equal("learner", access.Role);

        Assert.NotNull(okResult.Value);
        var payload = okResult.Value!;
        var responseAccess = AnonymousObjectReader.GetObject(payload, "access");
        Assert.NotNull(responseAccess);
        Assert.Equal(access.Id, AnonymousObjectReader.Get<int>(responseAccess, "accessId"));
        Assert.Equal(invitedUser.Id, AnonymousObjectReader.Get<int>(responseAccess, "userId"));
        Assert.Equal(invitedUser.Username, AnonymousObjectReader.Get<string>(responseAccess, "username"));
        Assert.Equal("learner", AnonymousObjectReader.Get<string>(responseAccess, "role"));
    }

    [Fact]
    public async Task InviteUser_ReturnsBadRequest_WhenInvitingSelf()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.InviteUser(new InviteDto
        {
            MapId = map.Id,
            Username = owner.Username,
            Role = "observer"
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Accesses);
    }

    [Fact]
    public async Task InviteUser_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.InviteUser(new InviteDto
        {
            MapId = 999,
            Username = "ghost",
            Role = "observer"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task InviteUser_ReturnsForbid_WhenRequesterIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var requester = CreateUser(2, "requester");
        var target = CreateUser(3, "target");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, requester, target);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(requester.Id, requester.Username);

        var result = await controller.InviteUser(new InviteDto
        {
            MapId = map.Id,
            Username = target.Username,
            Role = "learner"
        });

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(context.Accesses);
    }

    [Fact]
    public async Task InviteUser_ReturnsNotFound_WhenTargetUserDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.InviteUser(new InviteDto
        {
            MapId = map.Id,
            Username = "missing",
            Role = "observer"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task InviteUser_ReturnsBadRequest_WhenAccessAlreadyExists()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var target = CreateUser(2, "target");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, target);
        context.Maps.Add(map);
        context.Accesses.Add(new Access
        {
            Id = 100,
            MapId = map.Id,
            UserId = target.Id,
            Role = "observer"
        });
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.InviteUser(new InviteDto
        {
            MapId = map.Id,
            Username = target.Username,
            Role = "learner"
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(1, await context.Accesses.CountAsync());
    }

    [Fact]
    public async Task GetMapAccess_ReturnsFlattenedAccessListForOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var viewer = CreateUser(2, "viewer");
        var map = CreateMap(10, owner.Id);
        var access = new Access
        {
            Id = 100,
            MapId = map.Id,
            UserId = viewer.Id,
            Role = "observer"
        };

        context.Users.AddRange(owner, viewer);
        context.Maps.Add(map);
        context.Accesses.Add(access);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetMapAccess(map.Id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
        var entry = Assert.Single(payload);

        Assert.Equal(access.Id, AnonymousObjectReader.Get<int>(entry, "accessId"));
        Assert.Equal(viewer.Id, AnonymousObjectReader.Get<int>(entry, "userId"));
        Assert.Equal(viewer.Username, AnonymousObjectReader.Get<string>(entry, "username"));
        Assert.Equal("observer", AnonymousObjectReader.Get<string>(entry, "role"));
    }

    [Fact]
    public async Task GetMapAccess_ReturnsForbid_WhenRequesterIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var requester = CreateUser(2, "requester");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, requester);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(requester.Id, requester.Username);

        var result = await controller.GetMapAccess(map.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetMapAccess_ReturnsNotFound_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetMapAccess(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateRole_ChangesAccessRoleForMapOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var access = new Access
        {
            Id = 100,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "observer"
        };

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Accesses.Add(access);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateRole(access.Id, new UpdateRoleDto { Role = "learner" });

        Assert.IsType<OkObjectResult>(result);
        var updatedAccess = await context.Accesses.SingleAsync();
        Assert.Equal("learner", updatedAccess.Role);
    }

    [Fact]
    public async Task UpdateRole_ReturnsNotFound_WhenAccessDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateRole(999, new UpdateRoleDto { Role = "learner" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateRole_ReturnsForbid_WhenRequesterIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var requester = CreateUser(2, "requester");
        var target = CreateUser(3, "target");
        var map = CreateMap(10, owner.Id);
        var access = new Access
        {
            Id = 100,
            MapId = map.Id,
            UserId = target.Id,
            Role = "observer"
        };

        context.Users.AddRange(owner, requester, target);
        context.Maps.Add(map);
        context.Accesses.Add(access);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(requester.Id, requester.Username);

        var result = await controller.UpdateRole(access.Id, new UpdateRoleDto { Role = "learner" });

        Assert.IsType<ForbidResult>(result);
        Assert.Equal("observer", (await context.Accesses.SingleAsync()).Role);
    }

    [Fact]
    public async Task RemoveAccess_DeletesAccessForMapOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var access = new Access
        {
            Id = 100,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        };

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Accesses.Add(access);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.RemoveAccess(access.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(context.Accesses);
    }

    [Fact]
    public async Task RemoveAccess_ReturnsNotFound_WhenAccessDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.RemoveAccess(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RemoveAccess_ReturnsForbid_WhenRequesterIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var requester = CreateUser(2, "requester");
        var target = CreateUser(3, "target");
        var map = CreateMap(10, owner.Id);
        var access = new Access
        {
            Id = 100,
            MapId = map.Id,
            UserId = target.Id,
            Role = "observer"
        };

        context.Users.AddRange(owner, requester, target);
        context.Maps.Add(map);
        context.Accesses.Add(access);
        await context.SaveChangesAsync();

        var controller = new AccessController(context).WithAuthenticatedUser(requester.Id, requester.Username);

        var result = await controller.RemoveAccess(access.Id);

        Assert.IsType<ForbidResult>(result);
        Assert.Single(context.Accesses);
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
