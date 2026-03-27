using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Repositories;
using KnowledgeMap.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeMap.Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccessController : BaseController
    {
        private readonly IAccessService _accessService;

        [ActivatorUtilitiesConstructor]
        public AccessController(IAccessService accessService)
        {
            _accessService = accessService;
        }

        public AccessController(ApplicationDbContext context)
            : this(new AccessService(new AccessRepository(context)))
        {
        }

        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser(InviteDto inviteDto)
        {
            var result = await _accessService.InviteUserAsync(GetCurrentUserId(), inviteDto);
            return HandleServiceResult(result);
        }

        [HttpGet("map/{mapId}")]
        public async Task<IActionResult> GetMapAccess(int mapId)
        {
            var result = await _accessService.GetMapAccessAsync(GetCurrentUserId(), mapId);
            return HandleServiceResult(result);
        }

        [HttpPut("{accessId}/role")]
        public async Task<IActionResult> UpdateRole(int accessId, UpdateRoleDto updateRoleDto)
        {
            var result = await _accessService.UpdateRoleAsync(GetCurrentUserId(), accessId, updateRoleDto);
            return HandleServiceResult(result);
        }

        [HttpDelete("{accessId}")]
        public async Task<IActionResult> RemoveAccess(int accessId)
        {
            var result = await _accessService.RemoveAccessAsync(GetCurrentUserId(), accessId);
            return HandleServiceResult(result);
        }
    }
}
