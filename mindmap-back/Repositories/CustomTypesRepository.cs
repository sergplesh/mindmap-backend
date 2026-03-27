using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeMap.Backend.Repositories
{
    public interface ICustomTypesRepository
    {
        Task<Map?> GetMapByIdAsync(int mapId);
        Task<bool> HasMapAccessAsync(int mapId, int userId);
        Task<List<NodeType>> GetSystemNodeTypesAsync();
        Task<List<NodeType>> GetCustomNodeTypesAsync(int mapId);
        Task<NodeType?> GetCustomNodeTypeAsync(int mapId, int typeId);
        Task<List<EdgeType>> GetSystemEdgeTypesAsync();
        Task<List<EdgeType>> GetCustomEdgeTypesAsync(int mapId);
        Task<EdgeType?> GetCustomEdgeTypeAsync(int mapId, int typeId);
        Task AddNodeTypeAsync(NodeType nodeType);
        Task AddEdgeTypeAsync(EdgeType edgeType);
        Task LoadNodeTypeFieldDefinitionsAsync(NodeType nodeType);
        Task<List<NodeFieldValue>> GetNodeFieldValuesByDefinitionIdsAsync(IEnumerable<int> definitionIds);
        Task<bool> IsNodeTypeUsedAsync(int nodeTypeId);
        Task<bool> IsEdgeTypeUsedAsync(int edgeTypeId);
        void RemoveNodeFieldValues(IEnumerable<NodeFieldValue> values);
        void RemoveNodeTypeOptions(IEnumerable<NodeTypeFieldOption> options);
        void RemoveNodeTypeDefinitions(IEnumerable<NodeTypeFieldDefinition> definitions);
        void RemoveNodeType(NodeType nodeType);
        void RemoveEdgeType(EdgeType edgeType);
        Task SaveChangesAsync();
    }

    public class CustomTypesRepository : ICustomTypesRepository
    {
        private readonly ApplicationDbContext _context;

        public CustomTypesRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Map?> GetMapByIdAsync(int mapId)
        {
            return _context.Maps.FindAsync(mapId).AsTask();
        }

        public async Task<bool> HasMapAccessAsync(int mapId, int userId)
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

        public Task<List<NodeType>> GetSystemNodeTypesAsync()
        {
            return _context.NodeTypes
                .Include(t => t.FieldDefinitions)
                    .ThenInclude(f => f.Options)
                .Where(t => t.IsSystem)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public Task<List<NodeType>> GetCustomNodeTypesAsync(int mapId)
        {
            return _context.NodeTypes
                .Include(t => t.FieldDefinitions)
                    .ThenInclude(f => f.Options)
                .Where(t => !t.IsSystem && t.MapId == mapId)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public Task<NodeType?> GetCustomNodeTypeAsync(int mapId, int typeId)
        {
            return _context.NodeTypes
                .Include(t => t.FieldDefinitions)
                    .ThenInclude(f => f.Options)
                .FirstOrDefaultAsync(t => t.Id == typeId && !t.IsSystem && t.MapId == mapId);
        }

        public Task<List<EdgeType>> GetSystemEdgeTypesAsync()
        {
            return _context.EdgeTypes
                .Where(t => t.IsSystem)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public Task<List<EdgeType>> GetCustomEdgeTypesAsync(int mapId)
        {
            return _context.EdgeTypes
                .Where(t => !t.IsSystem && t.MapId == mapId)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public Task<EdgeType?> GetCustomEdgeTypeAsync(int mapId, int typeId)
        {
            return _context.EdgeTypes
                .FirstOrDefaultAsync(t => t.Id == typeId && !t.IsSystem && t.MapId == mapId);
        }

        public async Task AddNodeTypeAsync(NodeType nodeType)
        {
            _context.NodeTypes.Add(nodeType);
            await _context.SaveChangesAsync();
        }

        public async Task AddEdgeTypeAsync(EdgeType edgeType)
        {
            _context.EdgeTypes.Add(edgeType);
            await _context.SaveChangesAsync();
        }

        public Task LoadNodeTypeFieldDefinitionsAsync(NodeType nodeType)
        {
            return _context.Entry(nodeType)
                .Collection(t => t.FieldDefinitions)
                .Query()
                .Include(f => f.Options)
                .LoadAsync();
        }

        public Task<List<NodeFieldValue>> GetNodeFieldValuesByDefinitionIdsAsync(IEnumerable<int> definitionIds)
        {
            var ids = definitionIds.ToList();
            return _context.NodeFieldValues
                .Where(v => ids.Contains(v.NodeTypeFieldDefinitionId))
                .ToListAsync();
        }

        public Task<bool> IsNodeTypeUsedAsync(int nodeTypeId)
        {
            return _context.Nodes.AnyAsync(n => n.TypeId == nodeTypeId);
        }

        public Task<bool> IsEdgeTypeUsedAsync(int edgeTypeId)
        {
            return _context.Edges.AnyAsync(e => e.TypeId == edgeTypeId);
        }

        public void RemoveNodeFieldValues(IEnumerable<NodeFieldValue> values)
        {
            _context.NodeFieldValues.RemoveRange(values);
        }

        public void RemoveNodeTypeOptions(IEnumerable<NodeTypeFieldOption> options)
        {
            _context.NodeTypeFieldOptions.RemoveRange(options);
        }

        public void RemoveNodeTypeDefinitions(IEnumerable<NodeTypeFieldDefinition> definitions)
        {
            _context.NodeTypeFieldDefinitions.RemoveRange(definitions);
        }

        public void RemoveNodeType(NodeType nodeType)
        {
            _context.NodeTypes.Remove(nodeType);
        }

        public void RemoveEdgeType(EdgeType edgeType)
        {
            _context.EdgeTypes.Remove(edgeType);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
