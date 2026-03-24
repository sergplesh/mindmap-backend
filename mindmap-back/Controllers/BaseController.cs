using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KnowledgeMap.Backend.Data;

namespace KnowledgeMap.Backend.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new UnauthorizedAccessException("Пользователь не авторизован");
            }
            return int.Parse(userIdClaim.Value);
        }

        protected async Task<bool> HasAccessToMap(ApplicationDbContext context, int mapId, int userId)
        {
            var map = await context.Maps.FindAsync(mapId);
            if (map == null) return false;

            if (map.OwnerId == userId) return true;

            return await context.Accesses
                .AnyAsync(a => a.MapId == mapId && a.UserId == userId);
        }
    }
}