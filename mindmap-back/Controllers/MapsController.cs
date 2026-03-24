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
    public class MapsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public MapsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyMaps()
        {
            var userId = GetCurrentUserId();

            var maps = await _context.Maps
                .Include(m => m.Owner)
                .Where(m => m.OwnerId == userId || _context.Accesses.Any(a => a.MapId == m.Id && a.UserId == userId))
                .Select(m => new MapDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    Emoji = m.Emoji,
                    OwnerId = m.OwnerId,
                    OwnerName = m.Owner.Username,
                    UserRole = m.OwnerId == userId
                        ? "owner"
                        : _context.Accesses
                            .Where(a => a.MapId == m.Id && a.UserId == userId)
                            .Select(a => a.Role)
                            .FirstOrDefault() ?? "observer",
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    NodesCount = _context.Nodes.Count(n => n.MapId == m.Id),
                    EdgesCount = _context.Edges.Count(e => e.SourceNode.MapId == m.Id)
                })
                .ToListAsync();

            return Ok(maps);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMap(int id)
        {
            var userId = GetCurrentUserId();

            var map = await _context.Maps
                .Include(m => m.Owner)
                .Include(m => m.Nodes)
                    .ThenInclude(n => n.Type)
                .Include(m => m.Nodes)
                    .ThenInclude(n => n.Questions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            if (!await HasAccessToMap(_context, id, userId))
            {
                return Forbid();
            }

            var edges = await QueryEdgesForMap(id)
                .ToListAsync();

            var userRole = await GetUserRoleAsync(map, userId);

            return Ok(new
            {
                map.Id,
                map.Title,
                map.Description,
                map.Emoji,
                OwnerId = map.OwnerId,
                OwnerName = map.Owner.Username,
                map.CreatedAt,
                map.UpdatedAt,
                UserRole = userRole,
                Nodes = map.Nodes.Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Description,
                    TypeId = TypeScopeMapper.GetSystemNodeTypeId(n.Type),
                    TypeName = n.Type?.Name ?? "Неизвестно",
                    TypeColor = n.Type?.Color ?? "#3b82f6",
                    IsCustomType = n.Type?.IsSystem == false,
                    CustomTypeId = TypeScopeMapper.GetCustomNodeTypeId(n.Type),
                    n.XPosition,
                    n.YPosition,
                    HasQuestions = n.Questions.Any()
                }),
                Edges = edges.Select(e => new
                {
                    e.Id,
                    e.SourceNodeId,
                    e.TargetNodeId,
                    TypeId = TypeScopeMapper.GetSystemEdgeTypeId(e.Type),
                    TypeName = e.Type?.Name ?? "Неизвестно",
                    TypeStyle = e.Type?.Style ?? "solid",
                    TypeLabel = e.Type?.Label ?? string.Empty,
                    TypeColor = e.Type?.Color ?? "#666666",
                    IsCustomType = e.Type?.IsSystem == false,
                    CustomTypeId = TypeScopeMapper.GetCustomEdgeTypeId(e.Type),
                    e.IsHierarchy
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateMap(CreateMapDto createMapDto)
        {
            var userId = GetCurrentUserId();

            var map = new Map
            {
                Title = createMapDto.Title,
                Description = createMapDto.Description,
                Emoji = createMapDto.Emoji ?? "🗺️",
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Maps.Add(map);
            await _context.SaveChangesAsync();

            var centralNode = new Node
            {
                MapId = map.Id,
                Title = map.Title,
                Description = string.Empty,
                XPosition = 0,
                YPosition = 0,
                RequiresQuiz = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Nodes.Add(centralNode);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMap), new { id = map.Id }, new
            {
                map.Id,
                map.Title,
                map.Description,
                map.Emoji,
                map.OwnerId,
                map.CreatedAt,
                map.UpdatedAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMap(int id, UpdateMapDto updateMapDto)
        {
            var userId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(id);
            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != userId)
            {
                return Forbid();
            }

            map.Title = updateMapDto.Title;
            map.Description = updateMapDto.Description;
            map.Emoji = updateMapDto.Emoji;
            map.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                map.Id,
                map.Title,
                map.Description,
                map.Emoji,
                map.UpdatedAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMap(int id)
        {
            var userId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(id);
            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != userId)
            {
                return Forbid();
            }

            var edges = await QueryEdgesForMap(id).ToListAsync();
            _context.Edges.RemoveRange(edges);

            _context.Maps.Remove(map);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Карта успешно удалена" });
        }

        [HttpGet("{mapId}/nodes")]
        public async Task<IActionResult> GetMapNodes(int mapId)
        {
            var userId = GetCurrentUserId();

            if (!await HasAccessToMap(_context, mapId, userId))
            {
                return Forbid();
            }

            var nodes = await _context.Nodes
                .Include(n => n.Type)
                .Include(n => n.Questions)
                .Where(n => n.MapId == mapId)
                .ToListAsync();

            var response = nodes.Select(n => new
            {
                n.Id,
                n.Title,
                n.Description,
                TypeId = TypeScopeMapper.GetSystemNodeTypeId(n.Type),
                CustomTypeId = TypeScopeMapper.GetCustomNodeTypeId(n.Type),
                TypeName = n.Type != null ? n.Type.Name : "Неизвестно",
                TypeColor = n.Type != null ? n.Type.Color : "#3b82f6",
                IsCustomType = n.Type != null && !n.Type.IsSystem,
                n.XPosition,
                n.YPosition,
                n.CreatedAt,
                n.UpdatedAt,
                HasQuestions = n.Questions.Any()
            });

            return Ok(response);
        }

        [HttpGet("{mapId}/edges")]
        public async Task<IActionResult> GetMapEdges(int mapId)
        {
            var userId = GetCurrentUserId();

            if (!await HasAccessToMap(_context, mapId, userId))
            {
                return Forbid();
            }

            var edges = await QueryEdgesForMap(mapId)
                .ToListAsync();

            var response = edges.Select(e => new
            {
                e.Id,
                e.SourceNodeId,
                SourceNodeTitle = e.SourceNode.Title,
                e.TargetNodeId,
                TargetNodeTitle = e.TargetNode.Title,
                TypeId = TypeScopeMapper.GetSystemEdgeTypeId(e.Type),
                CustomTypeId = TypeScopeMapper.GetCustomEdgeTypeId(e.Type),
                TypeName = e.Type != null ? e.Type.Name : "Неизвестно",
                TypeStyle = e.Type != null ? e.Type.Style : "solid",
                TypeLabel = e.Type != null ? e.Type.Label : string.Empty,
                TypeColor = e.Type != null ? e.Type.Color : "#666666",
                IsCustomType = e.Type != null && !e.Type.IsSystem,
                e.IsHierarchy,
                e.CreatedAt
            });

            return Ok(response);
        }

        [HttpGet("{id}/full")]
        public async Task<IActionResult> GetFullMap(int id)
        {
            var userId = GetCurrentUserId();

            if (!await HasAccessToMap(_context, id, userId))
            {
                return Forbid();
            }

            var map = await _context.Maps
                .Include(m => m.Owner)
                .Include(m => m.Nodes)
                    .ThenInclude(n => n.Type)
                .Include(m => m.Nodes)
                    .ThenInclude(n => n.Questions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            var edges = await QueryEdgesForMap(id).ToListAsync();
            var accessSnapshot = await MapLearningAccessResolver.BuildAsync(_context, id, userId, map);
            var userRole = accessSnapshot.UserRole;

            return Ok(new
            {
                map.Id,
                map.Title,
                map.Description,
                map.Emoji,
                Owner = new
                {
                    map.Owner.Id,
                    map.Owner.Username
                },
                map.CreatedAt,
                map.UpdatedAt,
                UserRole = userRole,
                TotalNodesCount = map.Nodes.Count,
                Nodes = map.Nodes.Select(n =>
                {
                    var nodeState = accessSnapshot.NodeStates.TryGetValue(n.Id, out var state)
                        ? state
                        : new NodeLearningState();

                    return new
                    {
                        n.Id,
                        Title = nodeState.IsVisible ? n.Title : null,
                        Description = nodeState.CanReadContent ? n.Description : null,
                        n.RequiresQuiz,
                        TypeId = TypeScopeMapper.GetSystemNodeTypeId(n.Type),
                        CustomTypeId = TypeScopeMapper.GetCustomNodeTypeId(n.Type),
                        Type = n.Type != null ? new
                        {
                            n.Type.Id,
                            n.Type.Name,
                            n.Type.Color,
                            n.Type.Shape,
                            n.Type.Icon,
                            IsCustom = !n.Type.IsSystem
                        } : null,
                        n.XPosition,
                        n.YPosition,
                        n.CreatedAt,
                        n.UpdatedAt,
                        HasQuestions = n.Questions.Any(),
                        IsUnlocked = nodeState.IsUnlocked,
                        IsVisible = nodeState.IsVisible,
                        Level = nodeState.Level
                    };
                }),
                Edges = edges.Select(e =>
                {
                    var sourceState = accessSnapshot.NodeStates.TryGetValue(e.SourceNodeId, out var source)
                        ? source
                        : new NodeLearningState();
                    var targetState = accessSnapshot.NodeStates.TryGetValue(e.TargetNodeId, out var target)
                        ? target
                        : new NodeLearningState();

                    return new
                    {
                        e.Id,
                        e.SourceNodeId,
                        e.TargetNodeId,
                        TypeId = TypeScopeMapper.GetSystemEdgeTypeId(e.Type),
                        CustomTypeId = TypeScopeMapper.GetCustomEdgeTypeId(e.Type),
                        Type = e.Type != null ? new
                        {
                            e.Type.Id,
                            e.Type.Name,
                            e.Type.Style,
                            e.Type.Label,
                            e.Type.Color,
                            IsCustom = !e.Type.IsSystem
                        } : null,
                        e.IsHierarchy,
                        IsVisible = sourceState.IsVisible && targetState.IsVisible
                    };
                })
            });
        }

        private IQueryable<Edge> QueryEdgesForMap(int mapId)
        {
            return _context.Edges
                .Include(e => e.SourceNode)
                .Include(e => e.TargetNode)
                .Include(e => e.Type)
                .Where(e => e.SourceNode.MapId == mapId && e.TargetNode.MapId == mapId);
        }

        private async Task<string> GetUserRoleAsync(Map map, int userId)
        {
            if (map.OwnerId == userId)
            {
                return "owner";
            }

            var access = await _context.Accesses
                .Where(a => a.MapId == map.Id && a.UserId == userId)
                .Select(a => a.Role)
                .FirstOrDefaultAsync();

            return access ?? "observer";
        }
    }
}
