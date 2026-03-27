using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeMap.Backend.Repositories
{
    public interface IQuestionsRepository
    {
        Task<Node?> GetNodeWithMapAsync(int nodeId);
        Task<bool> HasAccessToMapAsync(int mapId, int userId);
        Task<List<Question>> GetQuestionsForNodeAsync(int nodeId);
        Task AddQuestionAsync(Question question);
        Task AddAnswerOptionsAsync(IEnumerable<AnswerOption> options);
        Task<Question?> GetQuestionForUpdateAsync(int questionId);
        Task<bool> HasSelectionsForOptionIdsAsync(IEnumerable<int> optionIds);
        void RemoveAnswerOptions(IEnumerable<AnswerOption> options);
        Task SaveChangesAsync();
        Task<Question?> GetQuestionWithMapAsync(int questionId);
        void RemoveQuestion(Question question);
        Task<Node?> GetNodeForVerificationAsync(int nodeId);
        Task<bool> HasPassedAnswerResultAsync(int nodeId, int userId);
        Task AddAnswerResultAsync(AnswerResult answerResult);
        Task<AnswerResult?> GetLatestAnswerResultAsync(int nodeId, int userId);
    }

    public class QuestionsRepository : IQuestionsRepository
    {
        private readonly ApplicationDbContext _context;

        public QuestionsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Node?> GetNodeWithMapAsync(int nodeId)
        {
            return _context.Nodes
                .Include(n => n.Map)
                .FirstOrDefaultAsync(n => n.Id == nodeId);
        }

        public async Task<bool> HasAccessToMapAsync(int mapId, int userId)
        {
            var map = await _context.Maps.FindAsync(mapId);
            if (map == null)
            {
                return false;
            }

            if (map.OwnerId == userId)
            {
                return true;
            }

            return await _context.Accesses.AnyAsync(a => a.MapId == mapId && a.UserId == userId);
        }

        public Task<List<Question>> GetQuestionsForNodeAsync(int nodeId)
        {
            return _context.Questions
                .Include(q => q.AnswerOptions)
                .Where(q => q.NodeId == nodeId)
                .ToListAsync();
        }

        public async Task AddQuestionAsync(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
        }

        public async Task AddAnswerOptionsAsync(IEnumerable<AnswerOption> options)
        {
            _context.AnswerOptions.AddRange(options);
            await _context.SaveChangesAsync();
        }

        public Task<Question?> GetQuestionForUpdateAsync(int questionId)
        {
            return _context.Questions
                .Include(q => q.AnswerOptions)
                .Include(q => q.Node)
                    .ThenInclude(n => n.Map)
                .FirstOrDefaultAsync(q => q.Id == questionId);
        }

        public Task<bool> HasSelectionsForOptionIdsAsync(IEnumerable<int> optionIds)
        {
            var ids = optionIds.ToList();
            return _context.AnswerResultSelections.AnyAsync(selection => ids.Contains(selection.AnswerOptionId));
        }

        public void RemoveAnswerOptions(IEnumerable<AnswerOption> options)
        {
            _context.AnswerOptions.RemoveRange(options);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task<Question?> GetQuestionWithMapAsync(int questionId)
        {
            return _context.Questions
                .Include(q => q.Node)
                    .ThenInclude(n => n.Map)
                .FirstOrDefaultAsync(q => q.Id == questionId);
        }

        public void RemoveQuestion(Question question)
        {
            _context.Questions.Remove(question);
        }

        public Task<Node?> GetNodeForVerificationAsync(int nodeId)
        {
            return _context.Nodes
                .Include(n => n.Map)
                .Include(n => n.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(n => n.Id == nodeId);
        }

        public Task<bool> HasPassedAnswerResultAsync(int nodeId, int userId)
        {
            return _context.AnswerResults.AnyAsync(ar => ar.NodeId == nodeId && ar.UserId == userId && ar.IsPassed);
        }

        public async Task AddAnswerResultAsync(AnswerResult answerResult)
        {
            _context.AnswerResults.Add(answerResult);
            await _context.SaveChangesAsync();
        }

        public Task<AnswerResult?> GetLatestAnswerResultAsync(int nodeId, int userId)
        {
            return _context.AnswerResults
                .Include(result => result.Selections)
                .Include(result => result.Questions)
                    .ThenInclude(question => question.Options)
                .Where(result => result.NodeId == nodeId && result.UserId == userId)
                .OrderByDescending(result => result.CompletedAt)
                .ThenByDescending(result => result.Id)
                .FirstOrDefaultAsync();
        }
    }
}
