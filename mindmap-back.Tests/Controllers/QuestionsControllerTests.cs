using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class QuestionsControllerTests
{
    [Fact]
    public async Task GetNodeQuestions_HidesCorrectAnswersForLearner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var question = new Question
        {
            Id = 200,
            NodeId = root.Id,
            QuestionText = "Question?",
            QuestionType = "single_choice",
            AnswerOptions =
            [
                new AnswerOption { Id = 300, OptionText = "Correct", IsCorrect = true },
                new AnswerOption { Id = 301, OptionText = "Wrong", IsCorrect = false }
            ]
        };

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.Add(root);
        context.Questions.Add(question);
        context.Accesses.Add(new Access
        {
            Id = 500,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.GetNodeQuestions(root.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var questions = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        var entry = Assert.Single(questions);
        var answers = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(entry, "AnswerOptions"));
        var firstAnswer = Assert.Single(answers, answer => AnonymousObjectReader.Get<int>(answer, "Id") == 300);

        Assert.Null(AnonymousObjectReader.GetObject(firstAnswer, "IsCorrect"));
    }

    [Fact]
    public async Task VerifyAnswers_PassesMultipleChoiceAndPersistsAttemptSelections()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var child = CreateNode(101, map.Id, "Child");
        var question = new Question
        {
            Id = 200,
            NodeId = child.Id,
            QuestionText = "Pick all",
            QuestionType = "multiple_choice",
            AnswerOptions =
            [
                new AnswerOption { Id = 300, OptionText = "A", IsCorrect = true },
                new AnswerOption { Id = 301, OptionText = "B", IsCorrect = true },
                new AnswerOption { Id = 302, OptionText = "C", IsCorrect = false }
            ]
        };

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, child);
        context.Questions.Add(question);
        context.Accesses.Add(new Access
        {
            Id = 500,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        context.Edges.Add(new Edge
        {
            Id = 600,
            SourceNodeId = root.Id,
            TargetNodeId = child.Id,
            IsHierarchy = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.VerifyAnswers(new VerifyAnswersDto
        {
            NodeId = child.Id,
            Answers =
            [
                new UserAnswerDto
                {
                    QuestionId = question.Id,
                    SelectedOptionIds = [300, 301, 301]
                }
            ]
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;

        Assert.True(AnonymousObjectReader.Get<bool>(payload, "isPassed"));
        var attempt = await context.AnswerResults.Include(a => a.Selections).SingleAsync();
        Assert.True(attempt.IsPassed);
        Assert.Equal(2, attempt.Selections.Count);
        Assert.Equal(new[] { 300, 301 }, attempt.Selections.Select(s => s.AnswerOptionId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public async Task UpdateQuestion_ReturnsBadRequest_WhenRemovingOptionWithHistory()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");
        var question = new Question
        {
            Id = 200,
            NodeId = node.Id,
            QuestionText = "Question",
            QuestionType = "single_choice",
            AnswerOptions =
            [
                new AnswerOption { Id = 300, OptionText = "Keep", IsCorrect = true },
                new AnswerOption { Id = 301, OptionText = "Remove", IsCorrect = false }
            ]
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        context.Questions.Add(question);
        context.AnswerResults.Add(new AnswerResult
        {
            Id = 400,
            UserId = owner.Id,
            NodeId = node.Id,
            IsPassed = false,
            CompletedAt = DateTime.UtcNow,
            Selections =
            [
                new AnswerResultSelection
                {
                    Id = 401,
                    AnswerOptionId = 301
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateQuestion(question.Id, new UpdateQuestionDto
        {
            QuestionText = "Updated",
            QuestionType = "single_choice",
            AnswerOptions =
            [
                new AnswerOptionDto
                {
                    Id = 300,
                    OptionText = "Keep",
                    IsCorrect = true
                }
            ]
        });

        Assert.IsType<BadRequestObjectResult>(result);
        var options = await context.AnswerOptions.Where(o => o.QuestionId == question.Id).ToListAsync();
        Assert.Equal(2, options.Count);
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

    private static Node CreateNode(int id, int mapId, string title) => new()
    {
        Id = id,
        MapId = mapId,
        Title = title,
        XPosition = 0,
        YPosition = 0,
        Width = 200,
        Height = 80,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
