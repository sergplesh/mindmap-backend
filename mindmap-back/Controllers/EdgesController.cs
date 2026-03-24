using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeMap.Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EdgesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public EdgesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateEdge(CreateEdgeDto createEdgeDto)
        {
            var userId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(createEdgeDto.MapId);
            if (map == null)
            {
                return NotFound(new { message = "РљР°СЂС‚Р° РЅРµ РЅР°Р№РґРµРЅР°" });
            }

            if (map.OwnerId != userId)
            {
                return Forbid();
            }

            var sourceNode = await _context.Nodes.FindAsync(createEdgeDto.SourceNodeId);
            var targetNode = await _context.Nodes.FindAsync(createEdgeDto.TargetNodeId);

            if (sourceNode == null || targetNode == null)
            {
                return BadRequest(new { message = "РћРґРёРЅ РёР· СѓР·Р»РѕРІ РЅРµ СЃСѓС‰РµСЃС‚РІСѓРµС‚" });
            }

            if (sourceNode.MapId != createEdgeDto.MapId || targetNode.MapId != createEdgeDto.MapId)
            {
                return BadRequest(new { message = "РЈР·Р»С‹ РґРѕР»Р¶РЅС‹ РїСЂРёРЅР°РґР»РµР¶Р°С‚СЊ СѓРєР°Р·Р°РЅРЅРѕР№ РєР°СЂС‚Рµ" });
            }

            var existingEdge = await _context.Edges
                .FirstOrDefaultAsync(e =>
                    e.SourceNodeId == createEdgeDto.SourceNodeId &&
                    e.TargetNodeId == createEdgeDto.TargetNodeId);

            if (existingEdge != null)
            {
                return BadRequest(new { message = "РўР°РєР°СЏ СЃРІСЏР·СЊ СѓР¶Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚" });
            }

            var edgeType = await ResolveEdgeTypeAsync(createEdgeDto.MapId, createEdgeDto.TypeId, createEdgeDto.CustomTypeId);
            if (edgeType == null && (createEdgeDto.TypeId.HasValue || createEdgeDto.CustomTypeId.HasValue))
            {
                return BadRequest(new { message = "РЈРєР°Р·Р°РЅРЅС‹Р№ С‚РёРї СЃРІСЏР·Рё РЅРµ СЃСѓС‰РµСЃС‚РІСѓРµС‚" });
            }

            var edge = new Edge
            {
                SourceNodeId = createEdgeDto.SourceNodeId,
                TargetNodeId = createEdgeDto.TargetNodeId,
                TypeId = edgeType?.Id,
                Label = NormalizeLabel(createEdgeDto.Label),
                IsHierarchy = createEdgeDto.IsHierarchy,
                CreatedAt = DateTime.UtcNow
            };

            _context.Edges.Add(edge);
            await _context.SaveChangesAsync();

            var createdEdge = await LoadEdgeForResponseAsync(edge.Id);
            if (createdEdge == null)
            {
                return NotFound(new { message = "РЎРІСЏР·СЊ РЅРµ РЅР°Р№РґРµРЅР°" });
            }

            return CreatedAtAction(nameof(GetEdge), new { id = edge.Id }, BuildEdgeResponse(createdEdge));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEdge(int id)
        {
            var userId = GetCurrentUserId();

            var edge = await LoadEdgeForResponseAsync(id);
            if (edge == null)
            {
                return NotFound(new { message = "РЎРІСЏР·СЊ РЅРµ РЅР°Р№РґРµРЅР°" });
            }

            var mapId = edge.SourceNode.MapId;
            var hasAccess = await HasAccessToMap(_context, mapId, userId);
            if (!hasAccess)
            {
                return Forbid();
            }

            return Ok(BuildEdgeResponse(edge));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEdge(int id, UpdateEdgeDto updateEdgeDto)
        {
            var userId = GetCurrentUserId();

            var edge = await _context.Edges
                .Include(e => e.SourceNode)
                    .ThenInclude(n => n.Map)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (edge == null)
            {
                return NotFound(new { message = "РЎРІСЏР·СЊ РЅРµ РЅР°Р№РґРµРЅР°" });
            }

            if (edge.SourceNode.Map.OwnerId != userId)
            {
                return Forbid();
            }

            if (updateEdgeDto.TypeId.HasValue || updateEdgeDto.CustomTypeId.HasValue)
            {
                var edgeType = await ResolveEdgeTypeAsync(edge.SourceNode.MapId, updateEdgeDto.TypeId, updateEdgeDto.CustomTypeId);
                if (edgeType == null)
                {
                    return BadRequest(new { message = "Указанный тип связи не существует" });
                }

                edge.TypeId = edgeType.Id;
            }

            edge.Label = NormalizeLabel(updateEdgeDto.Label);
            await _context.SaveChangesAsync();

            return Ok(new { message = "РЎРІСЏР·СЊ СѓСЃРїРµС€РЅРѕ РѕР±РЅРѕРІР»РµРЅР°" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEdge(int id)
        {
            var userId = GetCurrentUserId();

            var edge = await _context.Edges
                .Include(e => e.SourceNode)
                    .ThenInclude(n => n.Map)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (edge == null)
            {
                return NotFound(new { message = "РЎРІСЏР·СЊ РЅРµ РЅР°Р№РґРµРЅР°" });
            }

            if (edge.SourceNode.Map.OwnerId != userId)
            {
                return Forbid();
            }

            _context.Edges.Remove(edge);
            await _context.SaveChangesAsync();

            return Ok(new { message = "РЎРІСЏР·СЊ СѓСЃРїРµС€РЅРѕ СѓРґР°Р»РµРЅР°" });
        }

        private async Task<Edge?> LoadEdgeForResponseAsync(int edgeId)
        {
            return await _context.Edges
                .Include(e => e.SourceNode)
                .Include(e => e.TargetNode)
                .Include(e => e.Type)
                .FirstOrDefaultAsync(e => e.Id == edgeId);
        }

        private async Task<EdgeType?> ResolveEdgeTypeAsync(int mapId, int? systemTypeId, int? customTypeId)
        {
            if (customTypeId.HasValue)
            {
                return await _context.EdgeTypes
                    .FirstOrDefaultAsync(t => t.Id == customTypeId.Value && !t.IsSystem && t.MapId == mapId);
            }

            if (systemTypeId.HasValue)
            {
                return await _context.EdgeTypes
                    .FirstOrDefaultAsync(t => t.Id == systemTypeId.Value && t.IsSystem);
            }

            return null;
        }

        private static string? NormalizeLabel(string? label)
        {
            return string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        }

        private static object BuildEdgeResponse(Edge edge)
        {
            return new
            {
                edge.Id,
                MapId = edge.SourceNode.MapId,
                edge.SourceNodeId,
                SourceNodeTitle = edge.SourceNode.Title,
                edge.TargetNodeId,
                TargetNodeTitle = edge.TargetNode.Title,
                TypeId = TypeScopeMapper.GetSystemEdgeTypeId(edge.Type),
                CustomTypeId = TypeScopeMapper.GetCustomEdgeTypeId(edge.Type),
                TypeName = edge.Type?.Name ?? "РќРµРёР·РІРµСЃС‚РЅРѕ",
                Label = string.IsNullOrWhiteSpace(edge.Label) ? (edge.Type?.Label ?? string.Empty) : edge.Label,
                TypeStyle = edge.Type?.Style ?? "solid",
                TypeLabel = edge.Type?.Label ?? string.Empty,
                TypeColor = edge.Type?.Color ?? "#666666",
                edge.IsHierarchy,
                edge.CreatedAt
            };
        }
    }
}
