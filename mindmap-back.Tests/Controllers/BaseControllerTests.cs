using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class BaseControllerTests
{
    [Fact]
    public void GetCurrentUserId_Throws_WhenUserIsNotAuthenticated()
    {
        using var context = TestDbContextFactory.CreateContext();
        var controller = new TestController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var exception = Assert.Throws<UnauthorizedAccessException>(() => controller.ReadCurrentUserId());

        Assert.Equal("Пользователь не авторизован", exception.Message);
    }

    [Fact]
    public async Task HasAccessToMap_ReturnsFalse_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var controller = new TestController(context);

        var hasAccess = await controller.CheckAccessAsync(999, 1);

        Assert.False(hasAccess);
    }

    [Fact]
    public async Task HasAccessToMap_ReturnsTrue_ForOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);

        context.Users.Add(owner);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new TestController(context);

        var hasAccess = await controller.CheckAccessAsync(map.Id, owner.Id);

        Assert.True(hasAccess);
    }

    [Fact]
    public async Task HasAccessToMap_ReturnsTrue_ForSharedUser()
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

        var controller = new TestController(context);

        var hasAccess = await controller.CheckAccessAsync(map.Id, learner.Id);

        Assert.True(hasAccess);
    }

    [Fact]
    public async Task HasAccessToMap_ReturnsFalse_ForOutsider()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = new TestController(context);

        var hasAccess = await controller.CheckAccessAsync(map.Id, outsider.Id);

        Assert.False(hasAccess);
    }

    private sealed class TestController(ApplicationDbContext context) : BaseController
    {
        public int ReadCurrentUserId() => GetCurrentUserId();

        public Task<bool> CheckAccessAsync(int mapId, int userId) => HasAccessToMap(context, mapId, userId);
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
