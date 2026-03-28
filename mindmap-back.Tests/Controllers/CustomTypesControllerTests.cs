using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public sealed class CustomTypesControllerTests : ServiceTestBase
{
    [Fact]
    public async Task GetCustomNodeTypes_ReturnsOk()
    {
        await using var context = CreateContext();
        var owner = new User { Username = "owner_custom_ctrl", PasswordHash = "hash" };
        context.Users.Add(owner);
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

        var controller = WithUser(new CustomTypesController(context), owner.Id);
        var result = await controller.GetCustomNodeTypes(map.Id);

        Assert.IsType<OkObjectResult>(result);
    }
}
