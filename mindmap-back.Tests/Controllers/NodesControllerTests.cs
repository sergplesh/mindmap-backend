using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public sealed class NodesControllerTests : ServiceTestBase
{
    [Fact]
    public async Task CreateNode_ReturnsCreatedAtAction()
    {
        await using var context = CreateContext();
        var owner = new User { Username = "owner_node_ctrl", PasswordHash = "hash" };
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

        var controller = WithUser(new NodesController(context), owner.Id);
        var result = await controller.CreateNode(new CreateNodeDto
        {
            MapId = map.Id,
            Title = "Node from controller"
        });

        Assert.IsType<CreatedAtActionResult>(result);
    }
}
