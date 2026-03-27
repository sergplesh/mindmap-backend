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
    public class MapsController : BaseController
    {
        private readonly IMapsService _mapsService;

        [ActivatorUtilitiesConstructor]
        public MapsController(IMapsService mapsService)
        {
            _mapsService = mapsService;
        }

        public MapsController(ApplicationDbContext context)
            : this(new MapsService(
                new MapsRepository(context),
                new MapLearningAccessResolver(new MapLearningAccessRepository(context))))
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetMyMaps()
        {
            var result = await _mapsService.GetMyMapsAsync(GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMap(int id)
        {
            var result = await _mapsService.GetMapAsync(id, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMap(CreateMapDto createMapDto)
        {
            var result = await _mapsService.CreateMapAsync(GetCurrentUserId(), createMapDto);
            return HandleCreatedAtAction(result, nameof(GetMap));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMap(int id, UpdateMapDto updateMapDto)
        {
            var result = await _mapsService.UpdateMapAsync(id, GetCurrentUserId(), updateMapDto);
            return HandleServiceResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMap(int id)
        {
            var result = await _mapsService.DeleteMapAsync(id, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpGet("{mapId}/nodes")]
        public async Task<IActionResult> GetMapNodes(int mapId)
        {
            var result = await _mapsService.GetMapNodesAsync(mapId, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpGet("{mapId}/edges")]
        public async Task<IActionResult> GetMapEdges(int mapId)
        {
            var result = await _mapsService.GetMapEdgesAsync(mapId, GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpGet("{id}/full")]
        public async Task<IActionResult> GetFullMap(int id)
        {
            var result = await _mapsService.GetFullMapAsync(id, GetCurrentUserId());
            return HandleServiceResult(result);
        }
    }
}
