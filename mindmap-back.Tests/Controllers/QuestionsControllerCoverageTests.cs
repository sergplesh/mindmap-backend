using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class QuestionsControllerCoverageTests
{
    [Fact]
    public async Task GetNodeQuestions_ReturnsNotFound_WhenNodeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetNodeQuestions(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetNodeQuestions_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.GetNodeQuestions(node.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetNodeQuestions_ReturnsForbid_ForHiddenLearnerNode()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var quizNode = CreateNode(101, map.Id, "Quiz");
        var hiddenNode = CreateNode(102, map.Id, "Hidden");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, quizNode, hiddenNode);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        context.Questions.AddRange(
            new Question
            {
                Id = 300,
                NodeId = quizNode.Id,
                QuestionText = "Unlock?",
                QuestionType = "single_choice"
            },
            new Question
            {
                Id = 301,
                NodeId = hiddenNode.Id,
                QuestionText = "Hidden question",
                QuestionType = "single_choice"
            });
        context.Edges.AddRange(
            new Edge
            {
                Id = 400,
                SourceNodeId = root.Id,
                TargetNodeId = quizNode.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            },
            new Edge
            {
                Id = 401,
                SourceNodeId = quizNode.Id,
                TargetNodeId = hiddenNode.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.GetNodeQuestions(hiddenNode.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetNodeQuestions_OwnerSeesCorrectFlags()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        context.Questions.Add(new Question
        {
            Id = 200,
            NodeId = node.Id,
            QuestionText = "Question?",
            QuestionType = "single_choice",
            AnswerOptions =
            [
                new AnswerOption { Id = 300, OptionText = "Yes", IsCorrect = true },
                new AnswerOption { Id = 301, OptionText = "No", IsCorrect = false }
            ]
        });
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.GetNodeQuestions(node.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var questions = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        var answers = Assert.IsAssignableFrom<IEnumerable<object>>(AnonymousObjectReader.GetObject(Assert.Single(questions), "AnswerOptions"));
        var correct = Assert.Single(answers, answer => AnonymousObjectReader.Get<int>(answer, "Id") == 300);

        Assert.True(AnonymousObjectReader.Get<bool>(correct, "IsCorrect"));
    }

    [Fact]
    public async Task CreateQuestion_CreatesQuestionAndOptions()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateQuestion(new CreateQuestionDto
        {
            NodeId = node.Id,
            QuestionText = "Pick one",
            QuestionType = "single_choice",
            AnswerOptions =
            [
                new AnswerOptionDto { OptionText = "A", IsCorrect = true },
                new AnswerOptionDto { OptionText = "B", IsCorrect = false }
            ]
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Pick one", AnonymousObjectReader.Get<string>(ok.Value!, "QuestionText"));

        var savedQuestion = await context.Questions.Include(q => q.AnswerOptions).SingleAsync();
        Assert.Equal(2, savedQuestion.AnswerOptions.Count);
    }

    [Fact]
    public async Task CreateQuestion_ReturnsNotFound_WhenNodeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.CreateQuestion(new CreateQuestionDto
        {
            NodeId = 999,
            QuestionText = "Missing",
            QuestionType = "single_choice"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateQuestion_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.CreateQuestion(new CreateQuestionDto
        {
            NodeId = node.Id,
            QuestionText = "Denied",
            QuestionType = "single_choice"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateQuestion_ReturnsNotFound_WhenQuestionDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateQuestion(999, new UpdateQuestionDto
        {
            QuestionText = "Missing",
            QuestionType = "single_choice"
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateQuestion_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");
        var question = CreateQuestion(200, node.Id, "Original");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        context.Questions.Add(question);
        context.Accesses.Add(new Access
        {
            Id = 300,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.UpdateQuestion(question.Id, new UpdateQuestionDto
        {
            QuestionText = "Denied",
            QuestionType = "single_choice"
        });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateQuestion_ReturnsBadRequest_WhenIncomingOptionIdIsInvalid()
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
                new AnswerOption { Id = 300, OptionText = "A", IsCorrect = true }
            ]
        };

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        context.Questions.Add(question);
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
                    Id = 999,
                    OptionText = "Ghost",
                    IsCorrect = false
                }
            ]
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateQuestion_UpdatesExistingAddsNewAndRemovesUnusedOptions()
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
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.UpdateQuestion(question.Id, new UpdateQuestionDto
        {
            QuestionText = "Updated",
            QuestionType = "multiple_choice",
            AnswerOptions =
            [
                new AnswerOptionDto
                {
                    Id = 300,
                    OptionText = "  Updated keep  ",
                    IsCorrect = false
                },
                new AnswerOptionDto
                {
                    OptionText = " Added ",
                    IsCorrect = true
                },
                new AnswerOptionDto
                {
                    OptionText = "   ",
                    IsCorrect = false
                }
            ]
        });

        Assert.IsType<OkObjectResult>(result);

        var savedQuestion = await context.Questions
            .Include(q => q.AnswerOptions)
            .SingleAsync(q => q.Id == question.Id);

        Assert.Equal("Updated", savedQuestion.QuestionText);
        Assert.Equal("multiple_choice", savedQuestion.QuestionType);
        Assert.Equal(2, savedQuestion.AnswerOptions.Count);
        Assert.Contains(savedQuestion.AnswerOptions, option => option.Id == 300 && option.OptionText == "Updated keep" && !option.IsCorrect);
        Assert.Contains(savedQuestion.AnswerOptions, option => option.OptionText == "Added" && option.IsCorrect);
        Assert.DoesNotContain(savedQuestion.AnswerOptions, option => option.Id == 301);
    }

    [Fact]
    public async Task DeleteQuestion_RemovesQuestion()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");
        var question = CreateQuestion(200, node.Id, "Question");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        context.Questions.Add(question);
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteQuestion(question.Id);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(await context.Questions.ToListAsync());
    }

    [Fact]
    public async Task DeleteQuestion_ReturnsNotFound_WhenQuestionDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.DeleteQuestion(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteQuestion_ReturnsForbid_WhenUserIsNotOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");
        var question = CreateQuestion(200, node.Id, "Question");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        context.Questions.Add(question);
        context.Accesses.Add(new Access
        {
            Id = 300,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.DeleteQuestion(question.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task VerifyAnswers_ReturnsNotFound_WhenNodeDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var learner = CreateUser(1, "learner");
        context.Users.Add(learner);
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.VerifyAnswers(new VerifyAnswersDto { NodeId = 999 });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task VerifyAnswers_ReturnsForbid_WhenUserHasNoAccess()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var outsider = CreateUser(2, "outsider");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.AddRange(owner, outsider);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(outsider.Id, outsider.Username);

        var result = await controller.VerifyAnswers(new VerifyAnswersDto { NodeId = node.Id });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task VerifyAnswers_ReturnsForbid_ForObserver()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var observer = CreateUser(2, "observer");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var child = CreateNode(101, map.Id, "Child");

        context.Users.AddRange(owner, observer);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, child);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = observer.Id,
            Role = "observer"
        });
        context.Questions.Add(new Question
        {
            Id = 300,
            NodeId = child.Id,
            QuestionText = "Question",
            QuestionType = "single_choice"
        });
        context.Edges.Add(new Edge
        {
            Id = 400,
            SourceNodeId = root.Id,
            TargetNodeId = child.Id,
            IsHierarchy = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(observer.Id, observer.Username);

        var result = await controller.VerifyAnswers(new VerifyAnswersDto { NodeId = child.Id });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task VerifyAnswers_ReturnsAutoPass_ForOwner()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var node = CreateNode(100, map.Id, "Node");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(owner.Id, owner.Username);

        var result = await controller.VerifyAnswers(new VerifyAnswersDto { NodeId = node.Id });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(AnonymousObjectReader.Get<bool>(ok.Value!, "isPassed"));
        Assert.Empty(await context.AnswerResults.ToListAsync());
    }

    [Fact]
    public async Task VerifyAnswers_ReturnsAlreadyOpen_WhenNodeAlreadyPassed()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var child = CreateNode(101, map.Id, "Child");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, child);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        context.Questions.Add(new Question
        {
            Id = 300,
            NodeId = child.Id,
            QuestionText = "Question",
            QuestionType = "single_choice"
        });
        context.Edges.Add(new Edge
        {
            Id = 400,
            SourceNodeId = root.Id,
            TargetNodeId = child.Id,
            IsHierarchy = true,
            CreatedAt = DateTime.UtcNow
        });
        context.AnswerResults.Add(new AnswerResult
        {
            Id = 500,
            UserId = learner.Id,
            NodeId = child.Id,
            IsPassed = true,
            CompletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.VerifyAnswers(new VerifyAnswersDto
        {
            NodeId = child.Id,
            Answers =
            [
                new UserAnswerDto { QuestionId = 300 }
            ]
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Узел уже открыт", AnonymousObjectReader.Get<string>(ok.Value!, "message"));
        Assert.Single(await context.AnswerResults.ToListAsync());
    }

    [Fact]
    public async Task VerifyAnswers_ReturnsAlreadyOpen_WhenNodeIsAlreadyUnlocked()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.Add(root);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        context.Questions.Add(new Question
        {
            Id = 300,
            NodeId = root.Id,
            QuestionText = "Question",
            QuestionType = "single_choice"
        });
        await context.SaveChangesAsync();

        var controller = new QuestionsController(context).WithAuthenticatedUser(learner.Id, learner.Username);

        var result = await controller.VerifyAnswers(new VerifyAnswersDto
        {
            NodeId = root.Id,
            Answers =
            [
                new UserAnswerDto { QuestionId = 300 }
            ]
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Узел уже открыт", AnonymousObjectReader.Get<string>(ok.Value!, "message"));
        Assert.Empty(await context.AnswerResults.ToListAsync());
    }

    [Fact]
    public async Task VerifyAnswers_FailsSingleChoiceAndPersistsAttempt()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var child = CreateNode(101, map.Id, "Child");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, child);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        context.Questions.Add(new Question
        {
            Id = 300,
            NodeId = child.Id,
            QuestionText = "Question",
            QuestionType = "single_choice",
            AnswerOptions =
            [
                new AnswerOption { Id = 400, OptionText = "Correct", IsCorrect = true },
                new AnswerOption { Id = 401, OptionText = "Wrong", IsCorrect = false }
            ]
        });
        context.Edges.Add(new Edge
        {
            Id = 500,
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
                    QuestionId = 300,
                    SelectedOptionId = 401
                }
            ]
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.False(AnonymousObjectReader.Get<bool>(ok.Value!, "isPassed"));
        var attempt = await context.AnswerResults.Include(a => a.Selections).SingleAsync();
        Assert.False(attempt.IsPassed);
        Assert.Single(attempt.Selections);
        Assert.Equal(401, attempt.Selections.Single().AnswerOptionId);
    }

    [Fact]
    public async Task VerifyAnswers_FailsWhenAnswerIsMissing()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var child = CreateNode(101, map.Id, "Child");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, child);
        context.Accesses.Add(new Access
        {
            Id = 200,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        context.Questions.Add(new Question
        {
            Id = 300,
            NodeId = child.Id,
            QuestionText = "Question",
            QuestionType = "single_choice",
            AnswerOptions =
            [
                new AnswerOption { Id = 400, OptionText = "Correct", IsCorrect = true }
            ]
        });
        context.Edges.Add(new Edge
        {
            Id = 500,
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
            Answers = []
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.False(AnonymousObjectReader.Get<bool>(ok.Value!, "isPassed"));

        var attempt = await context.AnswerResults.Include(a => a.Selections).SingleAsync();
        Assert.False(attempt.IsPassed);
        Assert.Empty(attempt.Selections);
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

    private static Question CreateQuestion(int id, int nodeId, string text) => new()
    {
        Id = id,
        NodeId = nodeId,
        QuestionText = text,
        QuestionType = "single_choice"
    };
}
