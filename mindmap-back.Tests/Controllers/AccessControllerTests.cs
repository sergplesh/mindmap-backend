using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public sealed class AccessControllerTests : ServiceTestBase
{
    [Fact]
    public async Task InviteUser_ReturnsOkForOwner()
    {
        await using var context = CreateContext();
        var owner = new User { Username = "owner_access", PasswordHash = "hash" };
        var invited = new User { Username = "invited_access", PasswordHash = "hash" };
        context.Users.AddRange(owner, invited);
        await context.SaveChangesAsync();

        var map = new Map
        {
            OwnerId = owner.Id,
            Title = "Map",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var controller = WithUser(new AccessController(context), owner.Id);
        var result = await controller.InviteUser(new InviteDto
        {
            MapId = map.Id,
            Username = invited.Username,
            Role = "learner"
        });

        Assert.IsType<OkObjectResult>(result);
    }
}
