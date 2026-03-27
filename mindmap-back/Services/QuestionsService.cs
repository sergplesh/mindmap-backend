using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;

namespace KnowledgeMap.Backend.Services
{
    public interface IQuestionsService
    {
        Task<ServiceResult> GetNodeQuestionsAsync(int nodeId, int userId);
        Task<ServiceResult> GetLatestAttemptAsync(int nodeId, int userId);
        Task<ServiceResult> CreateQuestionAsync(int userId, CreateQuestionDto dto);
        Task<ServiceResult> UpdateQuestionAsync(int questionId, int userId, UpdateQuestionDto dto);
        Task<ServiceResult> DeleteQuestionAsync(int questionId, int userId);
        Task<ServiceResult> VerifyAnswersAsync(int userId, VerifyAnswersDto dto);
    }

    public class QuestionsService : IQuestionsService
    {
        private readonly IQuestionsRepository _repository;
        private readonly IMapLearningAccessService _mapLearningAccessService;

        public QuestionsService(IQuestionsRepository repository, IMapLearningAccessService mapLearningAccessService)
        {
            _repository = repository;
            _mapLearningAccessService = mapLearningAccessService;
        }

        public async Task<ServiceResult> GetNodeQuestionsAsync(int nodeId, int userId)
        {
            var node = await _repository.GetNodeWithMapAsync(nodeId);
            if (node == null)
            {
                return ServiceResult.NotFound(new { message = "Р Р€Р В·Р ВµР В» Р Р…Р Вµ Р Р…Р В°Р в„–Р Т‘Р ВµР Р…" });
            }

            if (!await _repository.HasAccessToMapAsync(node.MapId, userId))
            {
                return ServiceResult.Forbidden();
            }

            var accessSnapshot = await _mapLearningAccessService.BuildAsync(node.MapId, userId);
            var userRole = accessSnapshot.UserRole;
            var isOwner = userRole == "owner";
            var isObserver = userRole == "observer";
            var nodeState = accessSnapshot.NodeStates.TryGetValue(nodeId, out var state)
                ? state
                : new NodeLearningState();

            if (!isOwner && !isObserver && !nodeState.IsVisible)
            {
                return ServiceResult.Forbidden();
            }

            var questions = await _repository.GetQuestionsForNodeAsync(nodeId);
            var response = questions.Select(q => new
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
            }).ToList();

            return ServiceResult.Success(response);
        }

        public async Task<ServiceResult> GetLatestAttemptAsync(int nodeId, int userId)
        {
            var node = await _repository.GetNodeForVerificationAsync(nodeId);
            if (node == null)
            {
                return ServiceResult.NotFound(new { message = "Р Р€Р В·Р ВµР В» Р Р…Р Вµ Р Р…Р В°Р в„–Р Т‘Р ВµР Р…" });
            }

            if (!await _repository.HasAccessToMapAsync(node.MapId, userId))
            {
                return ServiceResult.Forbidden();
            }

            var accessSnapshot = await _mapLearningAccessService.BuildAsync(node.MapId, userId);
            var userRole = accessSnapshot.UserRole;
            var isOwner = userRole == "owner";
            var isObserver = userRole == "observer";
            var nodeState = accessSnapshot.NodeStates.TryGetValue(node.Id, out var state)
                ? state
                : new NodeLearningState();

            if (!isOwner && !isObserver && !nodeState.IsVisible)
            {
                return ServiceResult.Forbidden();
            }

            var latestAttempt = await _repository.GetLatestAnswerResultAsync(nodeId, userId);
            return ServiceResult.Success(latestAttempt == null ? null : BuildAttemptResponse(node, latestAttempt));
        }

        public async Task<ServiceResult> CreateQuestionAsync(int userId, CreateQuestionDto dto)
        {
            var node = await _repository.GetNodeWithMapAsync(dto.NodeId);
            if (node == null)
            {
                return ServiceResult.NotFound(new { message = "Р Р€Р В·Р ВµР В» Р Р…Р Вµ Р Р…Р В°Р в„–Р Т‘Р ВµР Р…" });
            }

            if (node.Map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            var question = new Question
            {
                NodeId = dto.NodeId,
                QuestionText = dto.QuestionText,
                QuestionType = dto.QuestionType
            };

            await _repository.AddQuestionAsync(question);

            if (dto.AnswerOptions != null && dto.AnswerOptions.Any())
            {
                var options = dto.AnswerOptions.Select(opt => new AnswerOption
                {
                    QuestionId = question.Id,
                    OptionText = opt.OptionText,
                    IsCorrect = opt.IsCorrect
                }).ToList();

                await _repository.AddAnswerOptionsAsync(options);
            }

            return ServiceResult.Success(new
            {
                question.Id,
                question.QuestionText,
                question.QuestionType
            });
        }

        public async Task<ServiceResult> UpdateQuestionAsync(int questionId, int userId, UpdateQuestionDto dto)
        {
            var question = await _repository.GetQuestionForUpdateAsync(questionId);
            if (question == null)
            {
                return ServiceResult.NotFound(new { message = "Р вЂ™Р С•Р С—РЎР‚Р С•РЎРѓ Р Р…Р Вµ Р Р…Р В°Р в„–Р Т‘Р ВµР Р…" });
            }

            if (question.Node.Map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            question.QuestionText = dto.QuestionText;
            question.QuestionType = dto.QuestionType;

            if (dto.AnswerOptions != null)
            {
                var incomingOptions = dto.AnswerOptions
                    .Where(option => !string.IsNullOrWhiteSpace(option.OptionText))
                    .ToList();

                var incomingOptionIds = incomingOptions
                    .Where(option => option.Id.HasValue)
                    .Select(option => option.Id!.Value)
                    .ToHashSet();

                var optionsToRemove = question.AnswerOptions
                    .Where(option => !incomingOptionIds.Contains(option.Id))
                    .ToList();

                if (optionsToRemove.Count > 0)
                {
                    var optionIdsToRemove = optionsToRemove
                        .Select(option => option.Id)
                        .ToList();

                    var hasSelections = await _repository.HasSelectionsForOptionIdsAsync(optionIdsToRemove);
                    if (hasSelections)
                    {
                        return ServiceResult.BadRequest(new
                        {
                            message = "Р СњР ВµР В»РЎРЉР В·РЎРЏ РЎС“Р Т‘Р В°Р В»Р С‘РЎвЂљРЎРЉ Р Р†Р В°РЎР‚Р С‘Р В°Р Р…РЎвЂљ Р С•РЎвЂљР Р†Р ВµРЎвЂљР В°, Р С—Р С• Р С”Р С•РЎвЂљР С•РЎР‚Р С•Р СРЎС“ РЎС“Р В¶Р Вµ Р ВµРЎРѓРЎвЂљРЎРЉ Р С‘РЎРѓРЎвЂљР С•РЎР‚Р С‘РЎРЏ Р С—РЎР‚Р С•РЎвЂ¦Р С•Р В¶Р Т‘Р ВµР Р…Р С‘РЎРЏ. Р ВР В·Р СР ВµР Р…Р С‘РЎвЂљР Вµ Р ВµР С–Р С• РЎвЂљР ВµР С”РЎРѓРЎвЂљ Р С‘Р В»Р С‘ Р С•РЎРѓРЎвЂљР В°Р Р†РЎРЉРЎвЂљР Вµ Р Р†Р В°РЎР‚Р С‘Р В°Р Р…РЎвЂљ."
                        });
                    }

                    _repository.RemoveAnswerOptions(optionsToRemove);
                }

                foreach (var optionDto in incomingOptions)
                {
                    if (optionDto.Id.HasValue)
                    {
                        var existingOption = question.AnswerOptions
                            .FirstOrDefault(option => option.Id == optionDto.Id.Value);

                        if (existingOption == null)
                        {
                            return ServiceResult.BadRequest(new { message = "Р СњР ВµР С”Р С•РЎР‚РЎР‚Р ВµР С”РЎвЂљР Р…РЎвЂ№Р в„– Р Р†Р В°РЎР‚Р С‘Р В°Р Р…РЎвЂљ Р С•РЎвЂљР Р†Р ВµРЎвЂљР В°." });
                        }

                        existingOption.OptionText = optionDto.OptionText.Trim();
                        existingOption.IsCorrect = optionDto.IsCorrect;
                        continue;
                    }

                    question.AnswerOptions.Add(new AnswerOption
                    {
                        QuestionId = question.Id,
                        OptionText = optionDto.OptionText.Trim(),
                        IsCorrect = optionDto.IsCorrect
                    });
                }
            }

            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Р вЂ™Р С•Р С—РЎР‚Р С•РЎРѓ Р С•Р В±Р Р…Р С•Р Р†Р В»РЎвЂР Р…" });
        }

        public async Task<ServiceResult> DeleteQuestionAsync(int questionId, int userId)
        {
            var question = await _repository.GetQuestionWithMapAsync(questionId);
            if (question == null)
            {
                return ServiceResult.NotFound(new { message = "Р вЂ™Р С•Р С—РЎР‚Р С•РЎРѓ Р Р…Р Вµ Р Р…Р В°Р в„–Р Т‘Р ВµР Р…" });
            }

            if (question.Node.Map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            _repository.RemoveQuestion(question);
            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Р вЂ™Р С•Р С—РЎР‚Р С•РЎРѓ РЎС“Р Т‘Р В°Р В»РЎвЂР Р…" });
        }

        public async Task<ServiceResult> VerifyAnswersAsync(int userId, VerifyAnswersDto dto)
        {
            var node = await _repository.GetNodeForVerificationAsync(dto.NodeId);
            if (node == null)
            {
                return ServiceResult.NotFound(new { message = "Р Р€Р В·Р ВµР В» Р Р…Р Вµ Р Р…Р В°Р в„–Р Т‘Р ВµР Р…" });
            }

            if (!await _repository.HasAccessToMapAsync(node.MapId, userId))
            {
                return ServiceResult.Forbidden();
            }

            var accessSnapshot = await _mapLearningAccessService.BuildAsync(node.MapId, userId);
            var userRole = accessSnapshot.UserRole;
            var isOwner = userRole == "owner";
            var isObserver = userRole == "observer";
            var nodeState = accessSnapshot.NodeStates.TryGetValue(node.Id, out var state)
                ? state
                : new NodeLearningState();

            if (isOwner)
            {
                return ServiceResult.Success(new
                {
                    message = "Р вЂ™Р В»Р В°Р Т‘Р ВµР В»Р ВµРЎвЂ  Р С”Р В°РЎР‚РЎвЂљРЎвЂ№ Р С‘Р СР ВµР ВµРЎвЂљ Р Т‘Р С•РЎРѓРЎвЂљРЎС“Р С— Р С”Р С• Р Р†РЎРѓР ВµР С РЎС“Р В·Р В»Р В°Р С",
                    isPassed = true,
                    results = new List<object>()
                });
            }

            if (isObserver || !nodeState.IsVisible)
            {
                return ServiceResult.Forbidden();
            }

            var alreadyPassed = await _repository.HasPassedAnswerResultAsync(node.Id, userId);
            if (alreadyPassed || nodeState.IsUnlocked)
            {
                var latestAttempt = await _repository.GetLatestAnswerResultAsync(node.Id, userId);

                return ServiceResult.Success(new
                {
                    message = "\u0423\u0437\u0435\u043B \u0443\u0436\u0435 \u043E\u0442\u043A\u0440\u044B\u0442",
                    isPassed = true,
                    results = latestAttempt == null
                        ? new List<object>()
                        : ToResponseResults(BuildAttemptResults(node, latestAttempt)),
                    latestAttempt = latestAttempt == null ? null : BuildAttemptResponse(node, latestAttempt)
                });
            }

            var attempt = BuildAttempt(node, userId, dto);
            var evaluation = EvaluateAttempt(attempt);
            var allCorrect = evaluation.All(result => result.IsCorrect);
            attempt.IsPassed = allCorrect;

            await _repository.AddAnswerResultAsync(attempt);

            var latestAttemptResponse = BuildAttemptResponse(node, attempt, evaluation);
            var responseResults = ToResponseResults(evaluation);

            if (allCorrect)
            {
                return ServiceResult.Success(new
                {
                    message = "Р вЂ™РЎРѓР Вµ Р С•РЎвЂљР Р†Р ВµРЎвЂљРЎвЂ№ Р С—РЎР‚Р В°Р Р†Р С‘Р В»РЎРЉР Р…РЎвЂ№Р Вµ! Р Р€Р В·Р ВµР В» Р С•РЎвЂљР С”РЎР‚РЎвЂ№РЎвЂљ.",
                    isPassed = true,
                    results = responseResults,
                    latestAttempt = latestAttemptResponse
                });
            }

            return ServiceResult.Success(new
            {
                message = "Р вЂўРЎРѓРЎвЂљРЎРЉ Р Р…Р ВµР С—РЎР‚Р В°Р Р†Р С‘Р В»РЎРЉР Р…РЎвЂ№Р Вµ Р С•РЎвЂљР Р†Р ВµРЎвЂљРЎвЂ№. Р СџР С•Р С—РЎР‚Р С•Р В±РЎС“Р в„–РЎвЂљР Вµ РЎРѓР Р…Р С•Р Р†Р В°.",
                isPassed = false,
                results = responseResults,
                latestAttempt = latestAttemptResponse
            });
        }

        private static object BuildAttemptResponse(Node node, AnswerResult attempt, List<QuestionAttemptEvaluation>? evaluation = null)
        {
            var results = evaluation ?? BuildAttemptResults(node, attempt);

            return new
            {
                attempt.Id,
                attempt.NodeId,
                attempt.IsPassed,
                attempt.CompletedAt,
                Results = ToResponseResults(results)
            };
        }

        private static List<QuestionAttemptEvaluation> BuildAttemptResults(Node node, AnswerResult attempt)
        {
            return attempt.Questions.Count > 0
                ? EvaluateAttempt(attempt)
                : EvaluateAttempt(node, attempt.Selections);
        }

        private static List<object> ToResponseResults(IEnumerable<QuestionAttemptEvaluation> evaluation)
        {
            return evaluation
                .Select(result => (object)new
                {
                    result.Id,
                    result.QuestionText,
                    result.QuestionType,
                    result.IsCorrect,
                    result.SelectedOptionIds,
                    result.SelectedOptionTexts,
                    result.CorrectOptionIds,
                    result.CorrectOptionTexts
                })
                .ToList();
        }

        private static AnswerResult BuildAttempt(Node node, int userId, VerifyAnswersDto dto)
        {
            var answersByQuestionId = dto.Answers
                .GroupBy(answer => answer.QuestionId)
                .ToDictionary(group => group.Key, group => group.First());

            var attempt = new AnswerResult
            {
                UserId = userId,
                NodeId = node.Id,
                CompletedAt = DateTime.UtcNow
            };

            var orderedQuestions = node.Questions
                .OrderBy(question => question.Id)
                .ToList();

            for (var questionIndex = 0; questionIndex < orderedQuestions.Count; questionIndex++)
            {
                var question = orderedQuestions[questionIndex];
                answersByQuestionId.TryGetValue(question.Id, out var userAnswer);

                var selectedIds = new HashSet<int>();
                if (userAnswer?.SelectedOptionId.HasValue == true)
                {
                    selectedIds.Add(userAnswer.SelectedOptionId.Value);
                }

                if (userAnswer?.SelectedOptionIds != null)
                {
                    foreach (var optionId in userAnswer.SelectedOptionIds.Distinct())
                    {
                        selectedIds.Add(optionId);
                    }
                }

                var questionSnapshot = new AnswerResultQuestion
                {
                    QuestionId = question.Id,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    DisplayOrder = questionIndex
                };

                var orderedOptions = question.AnswerOptions
                    .OrderBy(option => option.Id)
                    .ToList();

                for (var optionIndex = 0; optionIndex < orderedOptions.Count; optionIndex++)
                {
                    var option = orderedOptions[optionIndex];
                    var isSelected = selectedIds.Contains(option.Id);

                    questionSnapshot.Options.Add(new AnswerResultQuestionOption
                    {
                        AnswerOptionId = option.Id,
                        OptionText = option.OptionText,
                        IsCorrect = option.IsCorrect,
                        IsSelected = isSelected,
                        DisplayOrder = optionIndex
                    });

                    if (isSelected)
                    {
                        attempt.Selections.Add(new AnswerResultSelection
                        {
                            AnswerOptionId = option.Id
                        });
                    }
                }

                attempt.Questions.Add(questionSnapshot);
            }

            return attempt;
        }

        private static List<QuestionAttemptEvaluation> EvaluateAttempt(Node node, IEnumerable<AnswerResultSelection> selections)
        {
            var selectedOptionIds = selections
                .Select(selection => selection.AnswerOptionId)
                .ToHashSet();

            return node.Questions
                .Select(question =>
                {
                    var selectedOptions = question.AnswerOptions
                        .Where(option => selectedOptionIds.Contains(option.Id))
                        .ToList();

                    var selectedIds = selectedOptions
                        .Select(option => option.Id)
                        .Distinct()
                        .OrderBy(id => id)
                        .ToList();

                    var correctOptions = question.AnswerOptions
                        .Where(option => option.IsCorrect)
                        .ToList();

                    var isCorrect = question.QuestionType == "multiple_choice"
                        ? correctOptions.Select(option => option.Id).ToHashSet().SetEquals(selectedIds)
                        : selectedIds.Count == 1 && correctOptions.Any(option => option.Id == selectedIds[0]);

                    return new QuestionAttemptEvaluation
                    {
                        Id = question.Id,
                        QuestionText = question.QuestionText,
                        QuestionType = question.QuestionType,
                        IsCorrect = isCorrect,
                        SelectedOptionIds = selectedIds,
                        SelectedOptionTexts = selectedOptions.Select(option => option.OptionText).ToList(),
                        CorrectOptionIds = correctOptions.Select(option => option.Id).ToList(),
                        CorrectOptionTexts = correctOptions.Select(option => option.OptionText).ToList()
                    };
                })
                .ToList();
        }

        private static List<QuestionAttemptEvaluation> EvaluateAttempt(AnswerResult attempt)
        {
            return attempt.Questions
                .OrderBy(question => question.DisplayOrder)
                .Select(question =>
                {
                    var orderedOptions = question.Options
                        .OrderBy(option => option.DisplayOrder)
                        .ToList();

                    var selectedOptions = orderedOptions
                        .Where(option => option.IsSelected)
                        .ToList();

                    var correctOptions = orderedOptions
                        .Where(option => option.IsCorrect)
                        .ToList();

                    var selectedIds = selectedOptions
                        .Where(option => option.AnswerOptionId.HasValue)
                        .Select(option => option.AnswerOptionId!.Value)
                        .ToList();

                    var correctIds = correctOptions
                        .Where(option => option.AnswerOptionId.HasValue)
                        .Select(option => option.AnswerOptionId!.Value)
                        .ToList();

                    var isCorrect = question.QuestionType == "multiple_choice"
                        ? orderedOptions.Where(option => option.IsCorrect).Select(option => option.DisplayOrder).ToHashSet()
                            .SetEquals(orderedOptions.Where(option => option.IsSelected).Select(option => option.DisplayOrder))
                        : selectedOptions.Count == 1 && selectedOptions[0].IsCorrect;

                    return new QuestionAttemptEvaluation
                    {
                        Id = question.QuestionId ?? question.Id,
                        QuestionText = question.QuestionText,
                        QuestionType = question.QuestionType,
                        IsCorrect = isCorrect,
                        SelectedOptionIds = selectedIds,
                        SelectedOptionTexts = selectedOptions.Select(option => option.OptionText).ToList(),
                        CorrectOptionIds = correctIds,
                        CorrectOptionTexts = correctOptions.Select(option => option.OptionText).ToList()
                    };
                })
                .ToList();
        }

        private sealed class QuestionAttemptEvaluation
        {
            public int Id { get; set; }
            public string QuestionText { get; set; } = string.Empty;
            public string QuestionType { get; set; } = string.Empty;
            public bool IsCorrect { get; set; }
            public List<int> SelectedOptionIds { get; set; } = new();
            public List<string> SelectedOptionTexts { get; set; } = new();
            public List<int> CorrectOptionIds { get; set; } = new();
            public List<string> CorrectOptionTexts { get; set; } = new();
        }
    }
}
