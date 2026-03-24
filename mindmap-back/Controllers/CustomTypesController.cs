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
    [Route("api/maps/{mapId}/[controller]")]
    public class CustomTypesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public CustomTypesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("node-types")]
        public async Task<IActionResult> GetCustomNodeTypes(int mapId)
        {
            var currentUserId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(mapId);
            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            if (!await HasMapAccessAsync(map, currentUserId))
            {
                return Forbid();
            }

            var systemTypes = await _context.NodeTypes
                .Include(t => t.FieldDefinitions)
                    .ThenInclude(f => f.Options)
                .Where(t => t.IsSystem)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var customTypes = await _context.NodeTypes
                .Include(t => t.FieldDefinitions)
                    .ThenInclude(f => f.Options)
                .Where(t => !t.IsSystem && t.MapId == mapId)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return Ok(new
            {
                system = systemTypes.Select(ToNodeTypeResponse),
                custom = customTypes.Select(ToNodeTypeResponse)
            });
        }

        [HttpGet("node-types/{id}")]
        public async Task<IActionResult> GetCustomNodeType(int mapId, int id)
        {
            var currentUserId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(mapId);
            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            if (!await HasMapAccessAsync(map, currentUserId))
            {
                return Forbid();
            }

            var customType = await _context.NodeTypes
                .Include(t => t.FieldDefinitions)
                    .ThenInclude(f => f.Options)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsSystem && t.MapId == mapId);

            if (customType == null)
            {
                return NotFound(new { message = "Тип не найден" });
            }

            return Ok(ToNodeTypeResponse(customType));
        }

        [HttpGet("edge-types")]
        public async Task<IActionResult> GetCustomEdgeTypes(int mapId)
        {
            var currentUserId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(mapId);
            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            if (!await HasMapAccessAsync(map, currentUserId))
            {
                return Forbid();
            }

            var systemTypes = await _context.EdgeTypes
                .Where(t => t.IsSystem)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var customTypes = await _context.EdgeTypes
                .Where(t => !t.IsSystem && t.MapId == mapId)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return Ok(new
            {
                system = systemTypes.Select(ToEdgeTypeResponse),
                custom = customTypes.Select(ToEdgeTypeResponse)
            });
        }

        [HttpGet("edge-types/{id}")]
        public async Task<IActionResult> GetCustomEdgeType(int mapId, int id)
        {
            var currentUserId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(mapId);
            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            if (!await HasMapAccessAsync(map, currentUserId))
            {
                return Forbid();
            }

            var customType = await _context.EdgeTypes
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsSystem && t.MapId == mapId);

            if (customType == null)
            {
                return NotFound(new { message = "Тип не найден" });
            }

            return Ok(ToEdgeTypeResponse(customType));
        }

        [HttpPost("node-types")]
        public async Task<IActionResult> CreateCustomNodeType(int mapId, [FromBody] CreateCustomNodeTypeDto dto)
        {
            var currentUserId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(mapId);
            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != currentUserId)
            {
                return Forbid();
            }

            var customType = new NodeType
            {
                MapId = mapId,
                Name = dto.Name,
                Color = dto.Color,
                Icon = dto.Icon,
                Shape = string.IsNullOrWhiteSpace(dto.Shape) ? "rect" : dto.Shape,
                Size = string.IsNullOrWhiteSpace(dto.Size) ? "medium" : dto.Size,
                IsSystem = false
            };

            _context.NodeTypes.Add(customType);
            await _context.SaveChangesAsync();

            await ReplaceNodeTypeFieldDefinitionsAsync(customType, dto.CustomFields);
            await _context.Entry(customType).Collection(t => t.FieldDefinitions).Query()
                .Include(f => f.Options)
                .LoadAsync();

            return Ok(ToNodeTypeResponse(customType));
        }

        [HttpPost("edge-types")]
        public async Task<IActionResult> CreateCustomEdgeType(int mapId, [FromBody] CreateCustomEdgeTypeDto dto)
        {
            var currentUserId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(mapId);
            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != currentUserId)
            {
                return Forbid();
            }

            var customType = new EdgeType
            {
                MapId = mapId,
                Name = dto.Name,
                Style = dto.Style,
                Label = dto.Label,
                Color = dto.Color,
                IsSystem = false
            };

            _context.EdgeTypes.Add(customType);
            await _context.SaveChangesAsync();

            return Ok(ToEdgeTypeResponse(customType));
        }

        [HttpPut("node-types/{id}")]
        public async Task<IActionResult> UpdateCustomNodeType(int mapId, int id, [FromBody] UpdateCustomNodeTypeDto dto)
        {
            var currentUserId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(mapId);
            if (map == null || map.OwnerId != currentUserId)
            {
                return Forbid();
            }

            var customType = await _context.NodeTypes
                .Include(t => t.FieldDefinitions)
                    .ThenInclude(f => f.Options)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsSystem && t.MapId == mapId);

            if (customType == null)
            {
                return NotFound(new { message = "Тип не найден" });
            }

            customType.Name = dto.Name;
            customType.Color = dto.Color;
            customType.Icon = dto.Icon;
            customType.Shape = string.IsNullOrWhiteSpace(dto.Shape) ? "rect" : dto.Shape;
            customType.Size = string.IsNullOrWhiteSpace(dto.Size) ? "medium" : dto.Size;

            await ReplaceNodeTypeFieldDefinitionsAsync(customType, dto.CustomFields);
            await _context.Entry(customType).Collection(t => t.FieldDefinitions).Query()
                .Include(f => f.Options)
                .LoadAsync();

            return Ok(ToNodeTypeResponse(customType));
        }

        [HttpPut("edge-types/{id}")]
        public async Task<IActionResult> UpdateCustomEdgeType(int mapId, int id, [FromBody] UpdateCustomEdgeTypeDto dto)
        {
            var currentUserId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(mapId);
            if (map == null || map.OwnerId != currentUserId)
            {
                return Forbid();
            }

            var customType = await _context.EdgeTypes
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsSystem && t.MapId == mapId);

            if (customType == null)
            {
                return NotFound(new { message = "Тип не найден" });
            }

            customType.Name = dto.Name;
            customType.Style = dto.Style;
            customType.Label = dto.Label;
            customType.Color = dto.Color;

            await _context.SaveChangesAsync();

            return Ok(ToEdgeTypeResponse(customType));
        }

        [HttpDelete("node-types/{id}")]
        public async Task<IActionResult> DeleteCustomNodeType(int mapId, int id)
        {
            var currentUserId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(mapId);
            if (map == null || map.OwnerId != currentUserId)
            {
                return Forbid();
            }

            var customType = await _context.NodeTypes
                .Include(t => t.FieldDefinitions)
                    .ThenInclude(f => f.Options)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsSystem && t.MapId == mapId);

            if (customType == null)
            {
                return NotFound(new { message = "Тип не найден" });
            }

            var isUsed = await _context.Nodes.AnyAsync(n => n.TypeId == id);
            if (isUsed)
            {
                return BadRequest(new { message = "Нельзя удалить тип, который используется узлами" });
            }

            var definitionIds = customType.FieldDefinitions.Select(f => f.Id).ToList();
            if (definitionIds.Count > 0)
            {
                var fieldValues = await _context.NodeFieldValues
                    .Where(v => definitionIds.Contains(v.NodeTypeFieldDefinitionId))
                    .ToListAsync();

                _context.NodeFieldValues.RemoveRange(fieldValues);
                _context.NodeTypeFieldOptions.RemoveRange(customType.FieldDefinitions.SelectMany(f => f.Options));
                _context.NodeTypeFieldDefinitions.RemoveRange(customType.FieldDefinitions);
            }

            _context.NodeTypes.Remove(customType);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Тип удалён" });
        }

        [HttpDelete("edge-types/{id}")]
        public async Task<IActionResult> DeleteCustomEdgeType(int mapId, int id)
        {
            var currentUserId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(mapId);
            if (map == null || map.OwnerId != currentUserId)
            {
                return Forbid();
            }

            var customType = await _context.EdgeTypes
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsSystem && t.MapId == mapId);

            if (customType == null)
            {
                return NotFound(new { message = "Тип не найден" });
            }

            var isUsed = await _context.Edges.AnyAsync(e => e.TypeId == id);
            if (isUsed)
            {
                return BadRequest(new { message = "Нельзя удалить тип, который используется связями" });
            }

            _context.EdgeTypes.Remove(customType);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Тип удалён" });
        }

        private async Task<bool> HasMapAccessAsync(Map map, int userId)
        {
            if (map.OwnerId == userId)
            {
                return true;
            }

            return await _context.Accesses.AnyAsync(a => a.MapId == map.Id && a.UserId == userId);
        }

        private async Task ReplaceNodeTypeFieldDefinitionsAsync(NodeType nodeType, List<CustomFieldDto>? fields)
        {
            var incomingFields = fields ?? new List<CustomFieldDto>();
            var incomingNames = incomingFields.Select(f => f.Name).ToHashSet(StringComparer.Ordinal);

            var definitionsToRemove = nodeType.FieldDefinitions
                .Where(definition => !incomingNames.Contains(definition.Name))
                .ToList();

            if (definitionsToRemove.Count > 0)
            {
                var definitionIds = definitionsToRemove.Select(d => d.Id).ToList();
                var fieldValues = await _context.NodeFieldValues
                    .Where(v => definitionIds.Contains(v.NodeTypeFieldDefinitionId))
                    .ToListAsync();

                _context.NodeFieldValues.RemoveRange(fieldValues);
                _context.NodeTypeFieldOptions.RemoveRange(definitionsToRemove.SelectMany(d => d.Options));
                _context.NodeTypeFieldDefinitions.RemoveRange(definitionsToRemove);
            }

            var existingDefinitions = nodeType.FieldDefinitions
                .Where(d => !definitionsToRemove.Contains(d))
                .ToDictionary(d => d.Name, StringComparer.Ordinal);

            for (var index = 0; index < incomingFields.Count; index++)
            {
                var field = incomingFields[index];

                if (!existingDefinitions.TryGetValue(field.Name, out var definition))
                {
                    definition = new NodeTypeFieldDefinition
                    {
                        NodeTypeId = nodeType.Id
                    };
                    nodeType.FieldDefinitions.Add(definition);
                }

                definition.Name = field.Name;
                definition.FieldType = field.Type;
                definition.IsRequired = field.Required;
                definition.DefaultValue = field.DefaultValue;
                definition.Placeholder = field.Placeholder;
                definition.Validation = field.Validation;
                definition.SortOrder = index;

                _context.NodeTypeFieldOptions.RemoveRange(definition.Options);
                definition.Options.Clear();

                var options = field.Options ?? new List<string>();
                for (var optionIndex = 0; optionIndex < options.Count; optionIndex++)
                {
                    definition.Options.Add(new NodeTypeFieldOption
                    {
                        Value = options[optionIndex],
                        SortOrder = optionIndex
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private static object ToNodeTypeResponse(NodeType type)
        {
            return new
            {
                type.Id,
                type.Name,
                type.Color,
                type.Icon,
                type.Shape,
                type.Size,
                type.IsSystem,
                CustomFields = NodeTypeFieldMapper.ToDtos(type.FieldDefinitions)
            };
        }

        private static object ToEdgeTypeResponse(EdgeType type)
        {
            return new
            {
                type.Id,
                type.Name,
                type.Style,
                type.Label,
                type.Color,
                type.IsSystem
            };
        }
    }
}