using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeMap.Backend.Repositories
{
    public interface IAccessRepository
    {
        Task<Map?> GetMapWithOwnerAsync(int mapId);
        Task<Map?> GetMapByIdAsync(int mapId);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<Access?> GetAccessByMapAndUserAsync(int mapId, int userId);
        Task AddAccessAsync(Access access);
        Task<List<Access>> GetAccessesForMapAsync(int mapId);
        Task<Access?> GetAccessWithMapAsync(int accessId);
        Task SaveChangesAsync();
        void RemoveAccess(Access access);
    }

    public class AccessRepository : IAccessRepository
    {
        private readonly ApplicationDbContext _context;

        public AccessRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Map?> GetMapWithOwnerAsync(int mapId)
        {
            return _context.Maps
                .Include(m => m.Owner)
                .FirstOrDefaultAsync(m => m.Id == mapId);
        }

        public Task<Map?> GetMapByIdAsync(int mapId)
        {
            return _context.Maps.FindAsync(mapId).AsTask();
        }

        public Task<User?> GetUserByUsernameAsync(string username)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public Task<Access?> GetAccessByMapAndUserAsync(int mapId, int userId)
        {
            return _context.Accesses.FirstOrDefaultAsync(a => a.MapId == mapId && a.UserId == userId);
        }

        public async Task AddAccessAsync(Access access)
        {
            _context.Accesses.Add(access);
            await _context.SaveChangesAsync();
        }

        public Task<List<Access>> GetAccessesForMapAsync(int mapId)
        {
            return _context.Accesses
                .Include(a => a.User)
                .Where(a => a.MapId == mapId)
                .ToListAsync();
        }

        public Task<Access?> GetAccessWithMapAsync(int accessId)
        {
            return _context.Accesses
                .Include(a => a.Map)
                .FirstOrDefaultAsync(a => a.Id == accessId);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public void RemoveAccess(Access access)
        {
            _context.Accesses.Remove(access);
        }
    }
}
