using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeMap.Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public QuestionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("node/{nodeId}")]
        public async Task<IActionResult> GetNodeQuestions(int nodeId)
        {
            var userId = GetCurrentUserId();

            var node = await _context.Nodes
                .Include(n => n.Map)
                .FirstOrDefaultAsync(n => n.Id == nodeId);

            if (node == null)
            {
                return NotFound(new { message = "Узел не найден" });
            }

            var hasAccess = await HasAccessToMap(_context, node.MapId, userId);
            if (!hasAccess)
            {
                return Forbid();
            }

            var accessSnapshot = await MapLearningAccessResolver.BuildAsync(_context, node.MapId, userId);
            var userRole = accessSnapshot.UserRole;
            var isOwner = userRole == "owner";
            var isObserver = userRole == "observer";
            var nodeState = accessSnapshot.NodeStates.TryGetValue(nodeId, out var state)
                ? state
                : new NodeLearningState();

            if (!isOwner && !isObserver && !nodeState.IsVisible)
            {
                return Forbid();
            }

            var questions = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Where(q => q.NodeId == nodeId)
                .Select(q => new
                {
                    q.Id,
                    q.QuestionText,
                    q.QuestionType,
                    AnswerOptions = q.AnswerOptions.Select(a => new
                    {
                        a.Id,
                        a.OptionText,
                        IsCorrect = isOwner ? a.IsCorrect : (bool?)null
                    })
                })
                .ToListAsync();

            return Ok(questions);
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuestion(CreateQuestionDto dto)
        {
            var userId = GetCurrentUserId();

            var node = await _context.Nodes
                .Include(n => n.Map)
                .FirstOrDefaultAsync(n => n.Id == dto.NodeId);

            if (node == null)
            {
                return NotFound(new { message = "Узел не найден" });
            }

            if (node.Map.OwnerId != userId)
            {
                return Forbid();
            }

            var question = new Question
            {
                NodeId = dto.NodeId,
                QuestionText = dto.QuestionText,
                QuestionType = dto.QuestionType
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            if (dto.AnswerOptions != null && dto.AnswerOptions.Any())
            {
                foreach (var opt in dto.AnswerOptions)
                {
                    _context.AnswerOptions.Add(new AnswerOption
                    {
                        QuestionId = question.Id,
                        OptionText = opt.OptionText,
                        IsCorrect = opt.IsCorrect
                    });
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                question.Id,
                question.QuestionText,
                question.QuestionType
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuestion(int id, UpdateQuestionDto dto)
        {
            var userId = GetCurrentUserId();

            var question = await _context.Questions
                .Include(q => q.Node)
                    .ThenInclude(n => n.Map)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound(new { message = "Вопрос не найден" });
            }

            if (question.Node.Map.OwnerId != userId)
            {
                return Forbid();
            }

            question.QuestionText = dto.QuestionText;
            question.QuestionType = dto.QuestionType;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Вопрос обновлён" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var userId = GetCurrentUserId();

            var question = await _context.Questions
                .Include(q => q.Node)
                    .ThenInclude(n => n.Map)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound(new { message = "Вопрос не найден" });
            }

            if (question.Node.Map.OwnerId != userId)
            {
                return Forbid();
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Вопрос удалён" });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyAnswers(VerifyAnswersDto dto)
        {
            var userId = GetCurrentUserId();

            var node = await _context.Nodes
                .Include(n => n.Map)
                .Include(n => n.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(n => n.Id == dto.NodeId);

            if (node == null)
            {
                return NotFound(new { message = "Узел не найден" });
            }

            var hasAccess = await HasAccessToMap(_context, node.MapId, userId);
            if (!hasAccess)
            {
                return Forbid();
            }

            var accessSnapshot = await MapLearningAccessResolver.BuildAsync(_context, node.MapId, userId);
            var userRole = accessSnapshot.UserRole;
            var isOwner = userRole == "owner";
            var isObserver = userRole == "observer";
            var nodeState = accessSnapshot.NodeStates.TryGetValue(node.Id, out var state)
                ? state
                : new NodeLearningState();

            if (isOwner)
            {
                return Ok(new
                {
                    message = "Владелец карты имеет доступ ко всем узлам",
                    isPassed = true,
                    results = new List<object>()
                });
            }

            if (isObserver || !nodeState.IsVisible)
            {
                return Forbid();
            }

            var alreadyPassed = await _context.AnswerResults
                .AnyAsync(ar => ar.NodeId == node.Id && ar.UserId == userId && ar.IsPassed);

            if (alreadyPassed || nodeState.IsUnlocked)
            {
                return Ok(new
                {
                    message = "Узел уже открыт",
                    isPassed = true,
                    results = new List<object>()
                });
            }

            var allCorrect = true;
            var results = new List<object>();

            foreach (var question in node.Questions)
            {
                var userAnswer = dto.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                if (userAnswer == null)
                {
                    allCorrect = false;
                    continue;
                }

                var isCorrect = false;

                if (question.QuestionType == "single_choice")
                {
                    var selectedOption = question.AnswerOptions
                        .FirstOrDefault(o => o.Id == userAnswer.SelectedOptionId);
                    isCorrect = selectedOption != null && selectedOption.IsCorrect;
                }
                else if (question.QuestionType == "multiple_choice")
                {
                    var correctOptionIds = question.AnswerOptions
                        .Where(o => o.IsCorrect)
                        .Select(o => o.Id)
                        .ToHashSet();

                    var selectedIds = userAnswer.SelectedOptionIds?.ToHashSet() ?? new HashSet<int>();
                    isCorrect = correctOptionIds.SetEquals(selectedIds);
                }

                results.Add(new
                {
                    question.Id,
                    question.QuestionText,
                    isCorrect
                });

                if (!isCorrect)
                {
                    allCorrect = false;
                }
            }

            var attempt = new AnswerResult
            {
                UserId = userId,
                NodeId = node.Id,
                IsPassed = allCorrect,
                CompletedAt = DateTime.UtcNow
            };

            foreach (var answer in dto.Answers)
            {
                if (answer.SelectedOptionId.HasValue)
                {
                    attempt.Selections.Add(new AnswerResultSelection
                    {
                        AnswerOptionId = answer.SelectedOptionId.Value
                    });
                }

                if (answer.SelectedOptionIds != null)
                {
                    foreach (var optionId in answer.SelectedOptionIds.Distinct())
                    {
                        attempt.Selections.Add(new AnswerResultSelection
                        {
                            AnswerOptionId = optionId
                        });
                    }
                }
            }

            _context.AnswerResults.Add(attempt);
            await _context.SaveChangesAsync();

            if (allCorrect)
            {
                return Ok(new
                {
                    message = "Все ответы правильные! Узел открыт.",
                    isPassed = true,
                    results
                });
            }

            return Ok(new
            {
                message = "Есть неправильные ответы. Попробуйте снова.",
                isPassed = false,
                results
            });
        }
    }
}
