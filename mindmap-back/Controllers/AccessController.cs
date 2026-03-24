using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.DTOs;

namespace KnowledgeMap.Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccessController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AccessController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/access/invite - пригласить пользователя на карту
        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser(InviteDto inviteDto)
        {
            var currentUserId = GetCurrentUserId();

            // Проверяем существование карты
            var map = await _context.Maps
                .Include(m => m.Owner)
                .FirstOrDefaultAsync(m => m.Id == inviteDto.MapId);

            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            // Только владелец может приглашать
            if (map.OwnerId != currentUserId)
            {
                return Forbid();
            }

            // Ищем пользователя по логину
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == inviteDto.Username);

            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            // Нельзя пригласить самого себя
            if (user.Id == currentUserId)
            {
                return BadRequest(new { message = "Нельзя пригласить самого себя" });
            }

            // Проверяем, есть ли уже доступ
            var existingAccess = await _context.Accesses
                .FirstOrDefaultAsync(a => a.MapId == inviteDto.MapId && a.UserId == user.Id);

            if (existingAccess != null)
            {
                return BadRequest(new { message = "У пользователя уже есть доступ к этой карте" });
            }

            // Создаём доступ
            var access = new Access
            {
                MapId = inviteDto.MapId,
                UserId = user.Id,
                Role = inviteDto.Role // "observer" или "learner"
            };

            _context.Accesses.Add(access);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Пользователь приглашён",
                access = new
                {
                    user.Id,
                    user.Username,
                    access.Role
                }
            });
        }

        // GET: api/access/map/{mapId} - получить всех пользователей с доступом к карте
        [HttpGet("map/{mapId}")]
        public async Task<IActionResult> GetMapAccess(int mapId)
        {
            var currentUserId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(mapId);
            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            // Только владелец может видеть список доступа
            if (map.OwnerId != currentUserId)
            {
                return Forbid();
            }

            var accesses = await _context.Accesses
                .Include(a => a.User)
                .Where(a => a.MapId == mapId)
                .Select(a => new
                {
                    a.Id,
                    User = new
                    {
                        a.User.Id,
                        a.User.Username
                    },
                    a.Role
                })
                .ToListAsync();

            // Преобразуем в более удобный формат
            var result = accesses.Select(a => new
            {
                id = a.Id,
                userId = a.User.Id,
                username = a.User.Username,
                role = a.Role
            }).ToList();

            return Ok(result);
        }

        // PUT: api/access/{accessId}/role - изменить роль пользователя
        [HttpPut("{accessId}/role")]
        public async Task<IActionResult> UpdateRole(int accessId, UpdateRoleDto updateRoleDto)
        {
            var currentUserId = GetCurrentUserId();

            var access = await _context.Accesses
                .Include(a => a.Map)
                .FirstOrDefaultAsync(a => a.Id == accessId);

            if (access == null)
            {
                return NotFound(new { message = "Доступ не найден" });
            }

            // Только владелец карты может менять роли
            if (access.Map.OwnerId != currentUserId)
            {
                return Forbid();
            }

            access.Role = updateRoleDto.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Роль обновлена" });
        }

        // DELETE: api/access/{accessId} - удалить доступ пользователя
        [HttpDelete("{accessId}")]
        public async Task<IActionResult> RemoveAccess(int accessId)
        {
            var currentUserId = GetCurrentUserId();

            var access = await _context.Accesses
                .Include(a => a.Map)
                .FirstOrDefaultAsync(a => a.Id == accessId);

            if (access == null)
            {
                return NotFound(new { message = "Доступ не найден" });
            }

            // Только владелец карты может удалять доступ
            if (access.Map.OwnerId != currentUserId)
            {
                return Forbid();
            }

            _context.Accesses.Remove(access);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Доступ удалён" });
        }
    }
}