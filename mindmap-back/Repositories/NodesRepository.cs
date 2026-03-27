using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeMap.Backend.Repositories
{
    public interface INodesRepository
    {
        Task<Map?> GetMapByIdAsync(int mapId);
        Task<NodeType?> ResolveNodeTypeAsync(int mapId, int? systemTypeId, int? customTypeId);
        Task AddNodeAsync(Node node);
        Task<Node?> GetNodeForResponseAsync(int nodeId);
        Task<bool> HasAccessToMapAsync(int mapId, int userId);
        Task<Node?> GetNodeWithMapAsync(int nodeId);
        Task SaveChangesAsync();
        Task<List<Edge>> GetEdgesForNodeAsync(int nodeId);
        void RemoveEdges(IEnumerable<Edge> edges);
        void RemoveNode(Node node);
        Task<Node?> GetNodeWithTypeInfoAsync(int nodeId);
        Task<List<NodeFieldValue>> GetNodeFieldValuesAsync(int nodeId);
        Task<List<NodeTypeFieldDefinition>> GetNodeTypeFieldDefinitionsAsync(int nodeTypeId);
        void RemoveNodeFieldValues(IEnumerable<NodeFieldValue> values);
        void AddNodeFieldValue(NodeFieldValue value);
    }

    public class NodesRepository : INodesRepository
    {
        private readonly ApplicationDbContext _context;

        public NodesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Map?> GetMapByIdAsync(int mapId)
        {
            return _context.Maps.FindAsync(mapId).AsTask();
        }

        public Task<NodeType?> ResolveNodeTypeAsync(int mapId, int? systemTypeId, int? customTypeId)
        {
            if (customTypeId.HasValue)
            {
                return _context.NodeTypes
                    .FirstOrDefaultAsync(t => t.Id == customTypeId.Value && !t.IsSystem && t.MapId == mapId);
            }

            if (systemTypeId.HasValue)
            {
                return _context.NodeTypes
                    .FirstOrDefaultAsync(t => t.Id == systemTypeId.Value && t.IsSystem);
            }

            return Task.FromResult<NodeType?>(null);
        }

        public async Task AddNodeAsync(Node node)
        {
            _context.Nodes.Add(node);
            await _context.SaveChangesAsync();
        }

        public Task<Node?> GetNodeForResponseAsync(int nodeId)
        {
            return _context.Nodes
                .Include(n => n.Type)
                    .ThenInclude(t => t!.FieldDefinitions)
                        .ThenInclude(f => f.Options)
                .Include(n => n.FieldValues)
                    .ThenInclude(v => v.FieldDefinition)
                .Include(n => n.Questions)
                    .ThenInclude(q => q.AnswerOptions)
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

        public Task<Node?> GetNodeWithMapAsync(int nodeId)
        {
            return _context.Nodes
                .Include(n => n.Map)
                .FirstOrDefaultAsync(n => n.Id == nodeId);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task<List<Edge>> GetEdgesForNodeAsync(int nodeId)
        {
            return _context.Edges
                .Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId)
                .ToListAsync();
        }

        public void RemoveEdges(IEnumerable<Edge> edges)
        {
            _context.Edges.RemoveRange(edges);
        }

        public void RemoveNode(Node node)
        {
            _context.Nodes.Remove(node);
        }

        public Task<Node?> GetNodeWithTypeInfoAsync(int nodeId)
        {
            return _context.Nodes
                .Include(n => n.Type)
                    .ThenInclude(t => t!.FieldDefinitions)
                        .ThenInclude(f => f.Options)
                .FirstOrDefaultAsync(n => n.Id == nodeId);
        }

        public Task<List<NodeFieldValue>> GetNodeFieldValuesAsync(int nodeId)
        {
            return _context.NodeFieldValues
                .Where(v => v.NodeId == nodeId)
                .ToListAsync();
        }

        public Task<List<NodeTypeFieldDefinition>> GetNodeTypeFieldDefinitionsAsync(int nodeTypeId)
        {
            return _context.NodeTypeFieldDefinitions
                .Where(f => f.NodeTypeId == nodeTypeId)
                .OrderBy(f => f.SortOrder)
                .ToListAsync();
        }

        public void RemoveNodeFieldValues(IEnumerable<NodeFieldValue> values)
        {
            _context.NodeFieldValues.RemoveRange(values);
        }

        public void AddNodeFieldValue(NodeFieldValue value)
        {
            _context.NodeFieldValues.Add(value);
        }
    }
}
