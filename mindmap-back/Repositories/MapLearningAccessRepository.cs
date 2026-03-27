using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeMap.Backend.Repositories
{
    public sealed class HierarchyEdgeLink
    {
        public int SourceNodeId { get; init; }
        public int TargetNodeId { get; init; }
    }

    public interface IMapLearningAccessRepository
    {
        Task<Map?> GetMapWithNodesAndQuestionsAsync(int mapId);
        Task<List<HierarchyEdgeLink>> GetHierarchyEdgesAsync(int mapId);
        Task<HashSet<int>> GetPassedNodeIdsAsync(int userId, IEnumerable<int> nodeIds);
        Task<string?> GetAccessRoleAsync(int mapId, int userId);
    }

    public class MapLearningAccessRepository : IMapLearningAccessRepository
    {
        private readonly ApplicationDbContext _context;

        public MapLearningAccessRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Map?> GetMapWithNodesAndQuestionsAsync(int mapId)
        {
            return _context.Maps
                .Include(m => m.Nodes)
                    .ThenInclude(n => n.Questions)
                .Include(m => m.Accesses)
                .FirstOrDefaultAsync(m => m.Id == mapId);
        }

        public Task<List<HierarchyEdgeLink>> GetHierarchyEdgesAsync(int mapId)
        {
            return _context.Edges
                .Where(e => e.IsHierarchy && e.SourceNode.MapId == mapId && e.TargetNode.MapId == mapId)
                .Select(e => new HierarchyEdgeLink
                {
                    SourceNodeId = e.SourceNodeId,
                    TargetNodeId = e.TargetNodeId
                })
                .ToListAsync();
        }

        public async Task<HashSet<int>> GetPassedNodeIdsAsync(int userId, IEnumerable<int> nodeIds)
        {
            var ids = nodeIds.ToList();
            var passedIds = await _context.AnswerResults
                .Where(ar => ar.UserId == userId && ar.IsPassed && ids.Contains(ar.NodeId))
                .Select(ar => ar.NodeId)
                .Distinct()
                .ToListAsync();

            return passedIds.ToHashSet();
        }

        public Task<string?> GetAccessRoleAsync(int mapId, int userId)
        {
            return _context.Accesses
                .Where(a => a.MapId == mapId && a.UserId == userId)
                .Select(a => a.Role)
                .FirstOrDefaultAsync();
        }
    }
}
