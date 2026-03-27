using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeMap.Backend.Repositories
{
    public sealed class AccessibleMapSummary
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Emoji { get; init; }
        public int OwnerId { get; init; }
        public string OwnerName { get; init; } = string.Empty;
        public string UserRole { get; init; } = "observer";
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public int NodesCount { get; init; }
        public int EdgesCount { get; init; }
    }

    public interface IMapsRepository
    {
        Task<List<AccessibleMapSummary>> GetAccessibleMapSummariesAsync(int userId);
        Task<Map?> GetMapDetailsAsync(int mapId);
        Task<Map?> GetMapByIdAsync(int mapId);
        Task<bool> HasAccessToMapAsync(int mapId, int userId);
        Task<List<Edge>> GetEdgesForMapAsync(int mapId);
        Task AddMapAsync(Map map);
        Task AddNodeAsync(Node node);
        Task SaveChangesAsync();
        void RemoveEdges(IEnumerable<Edge> edges);
        void RemoveMap(Map map);
        Task<List<Node>> GetNodesForMapAsync(int mapId);
        Task<string?> GetAccessRoleAsync(int mapId, int userId);
    }

    public class MapsRepository : IMapsRepository
    {
        private readonly ApplicationDbContext _context;

        public MapsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<AccessibleMapSummary>> GetAccessibleMapSummariesAsync(int userId)
        {
            return _context.Maps
                .Include(m => m.Owner)
                .Where(m => m.OwnerId == userId || _context.Accesses.Any(a => a.MapId == m.Id && a.UserId == userId))
                .Select(m => new AccessibleMapSummary
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    Emoji = m.Emoji,
                    OwnerId = m.OwnerId,
                    OwnerName = m.Owner.Username,
                    UserRole = m.OwnerId == userId
                        ? "owner"
                        : _context.Accesses
                            .Where(a => a.MapId == m.Id && a.UserId == userId)
                            .Select(a => a.Role)
                            .FirstOrDefault() ?? "observer",
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    NodesCount = _context.Nodes.Count(n => n.MapId == m.Id),
                    EdgesCount = _context.Edges.Count(e => e.SourceNode.MapId == m.Id)
                })
                .ToListAsync();
        }

        public Task<Map?> GetMapDetailsAsync(int mapId)
        {
            return _context.Maps
                .Include(m => m.Owner)
                .Include(m => m.Accesses)
                .Include(m => m.Nodes)
                    .ThenInclude(n => n.Type)
                .Include(m => m.Nodes)
                    .ThenInclude(n => n.Questions)
                .FirstOrDefaultAsync(m => m.Id == mapId);
        }

        public Task<Map?> GetMapByIdAsync(int mapId)
        {
            return _context.Maps.FindAsync(mapId).AsTask();
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

        public Task<List<Edge>> GetEdgesForMapAsync(int mapId)
        {
            return _context.Edges
                .Include(e => e.SourceNode)
                .Include(e => e.TargetNode)
                .Include(e => e.Type)
                .Where(e => e.SourceNode.MapId == mapId && e.TargetNode.MapId == mapId)
                .ToListAsync();
        }

        public async Task AddMapAsync(Map map)
        {
            _context.Maps.Add(map);
            await _context.SaveChangesAsync();
        }

        public async Task AddNodeAsync(Node node)
        {
            _context.Nodes.Add(node);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public void RemoveEdges(IEnumerable<Edge> edges)
        {
            _context.Edges.RemoveRange(edges);
        }

        public void RemoveMap(Map map)
        {
            _context.Maps.Remove(map);
        }

        public Task<List<Node>> GetNodesForMapAsync(int mapId)
        {
            return _context.Nodes
                .Include(n => n.Type)
                .Include(n => n.Questions)
                .Where(n => n.MapId == mapId)
                .ToListAsync();
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
