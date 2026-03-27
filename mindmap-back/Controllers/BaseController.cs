using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Services;

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

        protected IActionResult HandleServiceResult(ServiceResult result)
        {
            return result.Type switch
            {
                ServiceResultType.Success => result.Value == null ? Ok() : Ok(result.Value),
                ServiceResultType.BadRequest => result.Value == null ? BadRequest() : BadRequest(result.Value),
                ServiceResultType.NotFound => result.Value == null ? NotFound() : NotFound(result.Value),
                ServiceResultType.Forbidden => Forbid(),
                ServiceResultType.Unauthorized => result.Value == null ? Unauthorized() : Unauthorized(result.Value),
                ServiceResultType.Created => throw new InvalidOperationException("Use HandleCreatedAtAction for created results."),
                _ => throw new InvalidOperationException("Unsupported service result type.")
            };
        }

        protected IActionResult HandleCreatedAtAction(ServiceResult result, string actionName)
        {
            return result.Type == ServiceResultType.Created
                ? CreatedAtAction(actionName, result.RouteValues, result.Value)
                : HandleServiceResult(result);
        }
    }
}
