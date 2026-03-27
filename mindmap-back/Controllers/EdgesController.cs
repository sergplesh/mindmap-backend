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
    public class EdgesController : BaseController
    {
        private readonly IEdgesService _edgesService;

        [ActivatorUtilitiesConstructor]
        public EdgesController(IEdgesService edgesService)
        {
            _edgesService = edgesService;
        }

        public EdgesController(ApplicationDbContext context)
            : this(new EdgesService(new EdgesRepository(context)))
        {
        }

        [HttpPost]
        public async Task<IActionResult> CreateEdge(CreateEdgeDto createEdgeDto)
        {
            var result = await _edgesService.CreateEdgeAsync(GetCurrentUserId(), createEdgeDto);
            return HandleCreatedAtAction(result, nameof(GetEdge));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEdge(int id)
        {
            var result = await _edgesService.GetEdgeAsync(id, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEdge(int id, UpdateEdgeDto updateEdgeDto)
        {
            var result = await _edgesService.UpdateEdgeAsync(id, GetCurrentUserId(), updateEdgeDto);
            return HandleServiceResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEdge(int id)
        {
            var result = await _edgesService.DeleteEdgeAsync(id, GetCurrentUserId());
            return HandleServiceResult(result);
        }
    }
}
