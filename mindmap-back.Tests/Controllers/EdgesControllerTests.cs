using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public sealed class EdgesControllerTests : ServiceTestBase
{
    [Fact]
    public async Task CreateEdge_ReturnsCreatedAtAction()
    {
        await using var context = CreateContext();
        var owner = new User { Username = "owner_edge_ctrl", PasswordHash = "hash" };
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

        var source = new Node
        {
            MapId = map.Id,
            Title = "Source",
            XPosition = 0,
            YPosition = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var target = new Node
        {
            MapId = map.Id,
            Title = "Target",
            XPosition = 1,
            YPosition = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Nodes.AddRange(source, target);
        await context.SaveChangesAsync();

        var controller = WithUser(new EdgesController(context), owner.Id);
        var result = await controller.CreateEdge(new CreateEdgeDto
        {
            MapId = map.Id,
            SourceNodeId = source.Id,
            TargetNodeId = target.Id
        });

        Assert.IsType<CreatedAtActionResult>(result);
    }
}
