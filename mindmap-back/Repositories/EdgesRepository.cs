using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeMap.Backend.Repositories
{
    public interface IEdgesRepository
    {
        Task<Map?> GetMapByIdAsync(int mapId);
        Task<Node?> GetNodeByIdAsync(int nodeId);
        Task<Edge?> GetExistingEdgeAsync(int sourceNodeId, int targetNodeId);
        Task<EdgeType?> ResolveEdgeTypeAsync(int mapId, int? systemTypeId, int? customTypeId);
        Task AddEdgeAsync(Edge edge);
        Task<Edge?> GetEdgeForResponseAsync(int edgeId);
        Task<bool> HasAccessToMapAsync(int mapId, int userId);
        Task<Edge?> GetEdgeWithOwnerAsync(int edgeId);
        Task SaveChangesAsync();
        void RemoveEdge(Edge edge);
    }

    public class EdgesRepository : IEdgesRepository
    {
        private readonly ApplicationDbContext _context;

        public EdgesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Map?> GetMapByIdAsync(int mapId)
        {
            return _context.Maps.FindAsync(mapId).AsTask();
        }

        public Task<Node?> GetNodeByIdAsync(int nodeId)
        {
            return _context.Nodes.FindAsync(nodeId).AsTask();
        }

        public Task<Edge?> GetExistingEdgeAsync(int sourceNodeId, int targetNodeId)
        {
            return _context.Edges
                .FirstOrDefaultAsync(e => e.SourceNodeId == sourceNodeId && e.TargetNodeId == targetNodeId);
        }

        public Task<EdgeType?> ResolveEdgeTypeAsync(int mapId, int? systemTypeId, int? customTypeId)
        {
            if (customTypeId.HasValue)
            {
                return _context.EdgeTypes
                    .FirstOrDefaultAsync(t => t.Id == customTypeId.Value && !t.IsSystem && t.MapId == mapId);
            }

            if (systemTypeId.HasValue)
            {
                return _context.EdgeTypes
                    .FirstOrDefaultAsync(t => t.Id == systemTypeId.Value && t.IsSystem);
            }

            return Task.FromResult<EdgeType?>(null);
        }

        public async Task AddEdgeAsync(Edge edge)
        {
            _context.Edges.Add(edge);
            await _context.SaveChangesAsync();
        }

        public Task<Edge?> GetEdgeForResponseAsync(int edgeId)
        {
            return _context.Edges
                .Include(e => e.SourceNode)
                .Include(e => e.TargetNode)
                .Include(e => e.Type)
                .FirstOrDefaultAsync(e => e.Id == edgeId);
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

        public Task<Edge?> GetEdgeWithOwnerAsync(int edgeId)
        {
            return _context.Edges
                .Include(e => e.SourceNode)
                    .ThenInclude(n => n.Map)
                .FirstOrDefaultAsync(e => e.Id == edgeId);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public void RemoveEdge(Edge edge)
        {
            _context.Edges.Remove(edge);
        }
    }
}
