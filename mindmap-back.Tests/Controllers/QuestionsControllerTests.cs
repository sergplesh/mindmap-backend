using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public sealed class QuestionsControllerTests : ServiceTestBase
{
    [Fact]
    public async Task CreateQuestion_ReturnsOk()
    {
        await using var context = CreateContext();
        var owner = new User { Username = "owner_question_ctrl", PasswordHash = "hash" };
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

        var node = new Node
        {
            MapId = map.Id,
            Title = "Node",
            XPosition = 0,
            YPosition = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = WithUser(new QuestionsController(context), owner.Id);
        var result = await controller.CreateQuestion(new CreateQuestionDto
        {
            NodeId = node.Id,
            QuestionText = "Q?",
            QuestionType = "single_choice"
        });

        Assert.IsType<OkObjectResult>(result);
    }
}
