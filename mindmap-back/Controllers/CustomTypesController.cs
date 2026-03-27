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
    [Route("api/maps/{mapId}/[controller]")]
    public class CustomTypesController : BaseController
    {
        private readonly ICustomTypesService _customTypesService;

        [ActivatorUtilitiesConstructor]
        public CustomTypesController(ICustomTypesService customTypesService)
        {
            _customTypesService = customTypesService;
        }

        public CustomTypesController(ApplicationDbContext context)
            : this(new CustomTypesService(new CustomTypesRepository(context)))
        {
        }

        [HttpGet("node-types")]
        public async Task<IActionResult> GetCustomNodeTypes(int mapId)
        {
            var result = await _customTypesService.GetCustomNodeTypesAsync(mapId, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpGet("node-types/{id}")]
        public async Task<IActionResult> GetCustomNodeType(int mapId, int id)
        {
            var result = await _customTypesService.GetCustomNodeTypeAsync(mapId, id, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpGet("edge-types")]
        public async Task<IActionResult> GetCustomEdgeTypes(int mapId)
        {
            var result = await _customTypesService.GetCustomEdgeTypesAsync(mapId, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpGet("edge-types/{id}")]
        public async Task<IActionResult> GetCustomEdgeType(int mapId, int id)
        {
            var result = await _customTypesService.GetCustomEdgeTypeAsync(mapId, id, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpPost("node-types")]
        public async Task<IActionResult> CreateCustomNodeType(int mapId, [FromBody] CreateCustomNodeTypeDto dto)
        {
            var result = await _customTypesService.CreateCustomNodeTypeAsync(mapId, GetCurrentUserId(), dto);
            return HandleServiceResult(result);
        }

        [HttpPost("edge-types")]
        public async Task<IActionResult> CreateCustomEdgeType(int mapId, [FromBody] CreateCustomEdgeTypeDto dto)
        {
            var result = await _customTypesService.CreateCustomEdgeTypeAsync(mapId, GetCurrentUserId(), dto);
            return HandleServiceResult(result);
        }

        [HttpPut("node-types/{id}")]
        public async Task<IActionResult> UpdateCustomNodeType(int mapId, int id, [FromBody] UpdateCustomNodeTypeDto dto)
        {
            var result = await _customTypesService.UpdateCustomNodeTypeAsync(mapId, id, GetCurrentUserId(), dto);
            return HandleServiceResult(result);
        }

        [HttpPut("edge-types/{id}")]
        public async Task<IActionResult> UpdateCustomEdgeType(int mapId, int id, [FromBody] UpdateCustomEdgeTypeDto dto)
        {
            var result = await _customTypesService.UpdateCustomEdgeTypeAsync(mapId, id, GetCurrentUserId(), dto);
            return HandleServiceResult(result);
        }

        [HttpDelete("node-types/{id}")]
        public async Task<IActionResult> DeleteCustomNodeType(int mapId, int id)
        {
            var result = await _customTypesService.DeleteCustomNodeTypeAsync(mapId, id, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpDelete("edge-types/{id}")]
        public async Task<IActionResult> DeleteCustomEdgeType(int mapId, int id)
        {
            var result = await _customTypesService.DeleteCustomEdgeTypeAsync(mapId, id, GetCurrentUserId());
            return HandleServiceResult(result);
        }
    }
}
