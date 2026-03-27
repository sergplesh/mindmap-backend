using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Repositories;
using KnowledgeMap.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeMap.Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionsController : BaseController
    {
        private readonly IQuestionsService _questionsService;

        [ActivatorUtilitiesConstructor]
        public QuestionsController(IQuestionsService questionsService)
        {
            _questionsService = questionsService;
        }

        public QuestionsController(ApplicationDbContext context)
            : this(new QuestionsService(
                new QuestionsRepository(context),
                new MapLearningAccessResolver(new MapLearningAccessRepository(context))))
        {
        }

        [HttpGet("node/{nodeId}")]
        public async Task<IActionResult> GetNodeQuestions(int nodeId)
        {
            var result = await _questionsService.GetNodeQuestionsAsync(nodeId, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpGet("node/{nodeId}/latest-attempt")]
        public async Task<IActionResult> GetLatestAttempt(int nodeId)
        {
            var result = await _questionsService.GetLatestAttemptAsync(nodeId, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuestion(CreateQuestionDto dto)
        {
            var result = await _questionsService.CreateQuestionAsync(GetCurrentUserId(), dto);
            return HandleServiceResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuestion(int id, UpdateQuestionDto dto)
        {
            var result = await _questionsService.UpdateQuestionAsync(id, GetCurrentUserId(), dto);
            return HandleServiceResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var result = await _questionsService.DeleteQuestionAsync(id, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyAnswers(VerifyAnswersDto dto)
        {
            var result = await _questionsService.VerifyAnswersAsync(GetCurrentUserId(), dto);
            return HandleServiceResult(result);
        }
    }
}
