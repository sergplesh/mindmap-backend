using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;
using KnowledgeMap.Backend.Services;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Services;

public sealed class QuestionsServiceTests : ServiceTestBase
{
    private static async Task<(ApplicationDbContext context, QuestionsService service, User owner, User learner, User outsider, Map map, Node root, Node child, Question childQuestion)> CreateFixtureAsync()
    {
        var context = CreateContext();
        var (owner, learner, outsider, map, root, child) = await SeedBasicMapAsync(context, withChildQuestion: true);
        var service = new QuestionsService(
            new QuestionsRepository(context),
            new MapLearningAccessResolver(new MapLearningAccessRepository(context)));

        var childQuestion = await context.Questions.Include(q => q.AnswerOptions).SingleAsync(q => q.NodeId == child.Id);
        return (context, service, owner, learner, outsider, map, root, child, childQuestion);
    }

    [Fact]
    public async Task VerifyAnswers_Owner_IsAlwaysPassed()
    {
        var (context, service, owner, _, _, _, _, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.VerifyAnswersAsync(owner.Id, new VerifyAnswersDto { NodeId = child.Id });

            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.True(Get<bool>(result.Value!, "isPassed"));
        }
    }

    [Fact]
    public async Task VerifyAnswers_LearnerWrongAnswer_IsNotPassed()
    {
        var (context, service, _, learner, _, _, _, child, question) = await CreateFixtureAsync();
        await using (context)
        {
            var wrongOptionId = question.AnswerOptions.Single(o => !o.IsCorrect).Id;
            var result = await service.VerifyAnswersAsync(learner.Id, new VerifyAnswersDto
            {
                NodeId = child.Id,
                Answers = new List<UserAnswerDto> { new() { QuestionId = question.Id, SelectedOptionId = wrongOptionId } }
            });

            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.False(Get<bool>(result.Value!, "isPassed"));
        }
    }

    [Fact]
    public async Task VerifyAnswers_LearnerCorrectAnswer_IsPassed()
    {
        var (context, service, _, learner, _, _, _, child, question) = await CreateFixtureAsync();
        await using (context)
        {
            var correctOptionId = question.AnswerOptions.Single(o => o.IsCorrect).Id;
            var result = await service.VerifyAnswersAsync(learner.Id, new VerifyAnswersDto
            {
                NodeId = child.Id,
                Answers = new List<UserAnswerDto> { new() { QuestionId = question.Id, SelectedOptionId = correctOptionId } }
            });

            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.True(Get<bool>(result.Value!, "isPassed"));
        }
    }

    [Fact]
    public async Task VerifyAnswers_RepeatedAfterPass_DoesNotCreateNewAttempt()
    {
        var (context, service, _, learner, _, _, _, child, question) = await CreateFixtureAsync();
        await using (context)
        {
            var correctOptionId = question.AnswerOptions.Single(o => o.IsCorrect).Id;
            var wrongOptionId = question.AnswerOptions.Single(o => !o.IsCorrect).Id;

            await service.VerifyAnswersAsync(learner.Id, new VerifyAnswersDto
            {
                NodeId = child.Id,
                Answers = new List<UserAnswerDto> { new() { QuestionId = question.Id, SelectedOptionId = correctOptionId } }
            });

            var attemptsBeforeRepeat = context.AnswerResults.Count(ar => ar.NodeId == child.Id && ar.UserId == learner.Id);
            var repeated = await service.VerifyAnswersAsync(learner.Id, new VerifyAnswersDto
            {
                NodeId = child.Id,
                Answers = new List<UserAnswerDto> { new() { QuestionId = question.Id, SelectedOptionId = wrongOptionId } }
            });

            Assert.Equal(ServiceResultType.Success, repeated.Type);
            Assert.Equal(attemptsBeforeRepeat, context.AnswerResults.Count(ar => ar.NodeId == child.Id && ar.UserId == learner.Id));
        }
    }

    [Fact]
    public async Task VerifyAnswers_Observer_ReturnsForbidden()
    {
        var (context, service, _, _, outsider, map, _, child, _) = await CreateFixtureAsync();
        await using (context)
        {
            context.Accesses.Add(new Access { MapId = map.Id, UserId = outsider.Id, Role = "observer" });
            await context.SaveChangesAsync();

            var result = await service.VerifyAnswersAsync(outsider.Id, new VerifyAnswersDto { NodeId = child.Id });

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task GetLatestAttempt_AfterAttempt_ReturnsSuccess()
    {
        var (context, service, _, learner, _, _, _, child, question) = await CreateFixtureAsync();
        await using (context)
        {
            var correctOptionId = question.AnswerOptions.Single(o => o.IsCorrect).Id;
            await service.VerifyAnswersAsync(learner.Id, new VerifyAnswersDto
            {
                NodeId = child.Id,
                Answers = new List<UserAnswerDto> { new() { QuestionId = question.Id, SelectedOptionId = correctOptionId } }
            });

            var latest = await service.GetLatestAttemptAsync(child.Id, learner.Id);

            Assert.Equal(ServiceResultType.Success, latest.Type);
            Assert.NotNull(latest.Value);
        }
    }

    [Fact]
    public async Task CreateQuestion_NonOwner_ReturnsForbidden()
    {
        var (context, service, _, learner, _, _, root, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateQuestionAsync(learner.Id, new CreateQuestionDto
            {
                NodeId = root.Id,
                QuestionText = "Denied",
                QuestionType = "single_choice"
            });

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task CreateQuestion_Owner_ReturnsSuccess()
    {
        var (context, service, owner, _, _, _, root, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.CreateQuestionAsync(owner.Id, new CreateQuestionDto
            {
                NodeId = root.Id,
                QuestionText = "What?",
                QuestionType = "single_choice",
                AnswerOptions = new List<AnswerOptionDto>
                {
                    new() { OptionText = "A", IsCorrect = true },
                    new() { OptionText = "B", IsCorrect = false }
                }
            });

            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task UpdateQuestion_NotFound_ReturnsNotFound()
    {
        var (context, service, owner, _, _, _, _, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            var result = await service.UpdateQuestionAsync(999999, owner.Id, new UpdateQuestionDto
            {
                QuestionText = "x",
                QuestionType = "single_choice"
            });

            Assert.Equal(ServiceResultType.NotFound, result.Type);
        }
    }

    [Fact]
    public async Task UpdateQuestion_NonOwner_ReturnsForbidden()
    {
        var (context, service, owner, learner, _, _, root, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateQuestionAsync(owner.Id, new CreateQuestionDto
            {
                NodeId = root.Id,
                QuestionText = "What?",
                QuestionType = "single_choice"
            });
            var question = context.Questions.Single(q => q.NodeId == root.Id && q.QuestionText == "What?");

            var result = await service.UpdateQuestionAsync(question.Id, learner.Id, new UpdateQuestionDto
            {
                QuestionText = "x",
                QuestionType = "single_choice"
            });

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task UpdateQuestion_RemoveUsedOption_ReturnsBadRequest()
    {
        var (context, service, owner, learner, _, _, root, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateQuestionAsync(owner.Id, new CreateQuestionDto
            {
                NodeId = root.Id,
                QuestionText = "What?",
                QuestionType = "single_choice",
                AnswerOptions = new List<AnswerOptionDto>
                {
                    new() { OptionText = "A", IsCorrect = true },
                    new() { OptionText = "B", IsCorrect = false }
                }
            });

            var question = await context.Questions.Include(q => q.AnswerOptions).SingleAsync(q => q.NodeId == root.Id && q.QuestionText == "What?");
            var optionToProtect = question.AnswerOptions.First();

            context.AnswerResults.Add(new AnswerResult
            {
                UserId = learner.Id,
                NodeId = root.Id,
                IsPassed = false,
                CompletedAt = DateTime.UtcNow,
                Selections = new List<AnswerResultSelection> { new() { AnswerOptionId = optionToProtect.Id } }
            });
            await context.SaveChangesAsync();

            var result = await service.UpdateQuestionAsync(question.Id, owner.Id, new UpdateQuestionDto
            {
                QuestionText = "Still what?",
                QuestionType = "single_choice",
                AnswerOptions = question.AnswerOptions
                    .Where(o => o.Id != optionToProtect.Id)
                    .Select(o => new AnswerOptionDto { Id = o.Id, OptionText = o.OptionText, IsCorrect = o.IsCorrect })
                    .ToList()
            });

            Assert.Equal(ServiceResultType.BadRequest, result.Type);
        }
    }

    [Fact]
    public async Task UpdateQuestion_ValidPayload_ReturnsSuccess()
    {
        var (context, service, owner, _, _, _, root, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateQuestionAsync(owner.Id, new CreateQuestionDto
            {
                NodeId = root.Id,
                QuestionText = "What?",
                QuestionType = "single_choice",
                AnswerOptions = new List<AnswerOptionDto>
                {
                    new() { OptionText = "A", IsCorrect = true },
                    new() { OptionText = "B", IsCorrect = false }
                }
            });

            var question = await context.Questions.Include(q => q.AnswerOptions).SingleAsync(q => q.NodeId == root.Id && q.QuestionText == "What?");
            var result = await service.UpdateQuestionAsync(question.Id, owner.Id, new UpdateQuestionDto
            {
                QuestionText = "Updated question",
                QuestionType = "single_choice",
                AnswerOptions = question.AnswerOptions
                    .Select(o => new AnswerOptionDto { Id = o.Id, OptionText = $"{o.OptionText}_updated", IsCorrect = o.IsCorrect })
                    .Append(new AnswerOptionDto { OptionText = "C", IsCorrect = false })
                    .ToList()
            });

            Assert.Equal(ServiceResultType.Success, result.Type);
        }
    }

    [Fact]
    public async Task DeleteQuestion_NonOwner_ReturnsForbidden()
    {
        var (context, service, owner, learner, _, _, root, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateQuestionAsync(owner.Id, new CreateQuestionDto
            {
                NodeId = root.Id,
                QuestionText = "What?",
                QuestionType = "single_choice"
            });
            var question = context.Questions.Single(q => q.NodeId == root.Id && q.QuestionText == "What?");

            var result = await service.DeleteQuestionAsync(question.Id, learner.Id);

            Assert.Equal(ServiceResultType.Forbidden, result.Type);
        }
    }

    [Fact]
    public async Task DeleteQuestion_Owner_ReturnsSuccess()
    {
        var (context, service, owner, _, _, _, root, _, _) = await CreateFixtureAsync();
        await using (context)
        {
            await service.CreateQuestionAsync(owner.Id, new CreateQuestionDto
            {
                NodeId = root.Id,
                QuestionText = "What?",
                QuestionType = "single_choice"
            });
            var question = context.Questions.Single(q => q.NodeId == root.Id && q.QuestionText == "What?");

            var result = await service.DeleteQuestionAsync(question.Id, owner.Id);

            Assert.Equal(ServiceResultType.Success, result.Type);
            Assert.DoesNotContain(context.Questions, q => q.Id == question.Id);
        }
    }
}
