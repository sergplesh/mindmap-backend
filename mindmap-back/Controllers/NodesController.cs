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
    public class NodesController : BaseController
    {
        private readonly INodesService _nodesService;

        [ActivatorUtilitiesConstructor]
        public NodesController(INodesService nodesService)
        {
            _nodesService = nodesService;
        }

        public NodesController(ApplicationDbContext context)
            : this(new NodesService(
                new NodesRepository(context),
                new MapLearningAccessResolver(new MapLearningAccessRepository(context))))
        {
        }

        [HttpPost]
        public async Task<IActionResult> CreateNode([FromBody] CreateNodeDto createNodeDto)
        {
            var result = await _nodesService.CreateNodeAsync(GetCurrentUserId(), createNodeDto);
            return HandleCreatedAtAction(result, nameof(GetNode));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNode(int id)
        {
            var result = await _nodesService.GetNodeAsync(id, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNode(int id, [FromBody] UpdateNodeDto updateNodeDto)
        {
            var result = await _nodesService.UpdateNodeAsync(id, GetCurrentUserId(), updateNodeDto);
            return HandleServiceResult(result);
        }

        [HttpPatch("{id}/position")]
        public async Task<IActionResult> UpdateNodePosition(int id, [FromBody] UpdateNodePositionDto positionDto)
        {
            var result = await _nodesService.UpdateNodePositionAsync(id, GetCurrentUserId(), positionDto);
            return HandleServiceResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNode(int id)
        {
            var result = await _nodesService.DeleteNodeAsync(id, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpGet("{id}/type")]
        public async Task<IActionResult> GetNodeTypeInfo(int id)
        {
            var result = await _nodesService.GetNodeTypeInfoAsync(id, GetCurrentUserId());
            return HandleServiceResult(result);
        }
    }
}
