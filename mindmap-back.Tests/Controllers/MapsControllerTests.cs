using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public sealed class MapsControllerTests : ServiceTestBase
{
    [Fact]
    public async Task CreateMap_ReturnsCreatedAtAction()
    {
        await using var context = CreateContext();
        var owner = new User { Username = "owner_map_ctrl", PasswordHash = "hash" };
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = WithUser(new MapsController(context), owner.Id);
        var result = await controller.CreateMap(new CreateMapDto
        {
            Title = "Controller map",
            Description = "desc",
            Emoji = "M"
        });

        Assert.IsType<CreatedAtActionResult>(result);
    }
}
