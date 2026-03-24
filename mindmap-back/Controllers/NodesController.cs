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
    public class NodesController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public NodesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNode([FromBody] CreateNodeDto createNodeDto)
        {
            var userId = GetCurrentUserId();

            var map = await _context.Maps.FindAsync(createNodeDto.MapId);
            if (map == null)
            {
                return NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != userId)
            {
                return Forbid();
            }

            var nodeType = await ResolveNodeTypeAsync(createNodeDto.MapId, createNodeDto.TypeId, createNodeDto.CustomTypeId);
            if (nodeType == null && (createNodeDto.TypeId.HasValue || createNodeDto.CustomTypeId.HasValue))
            {
                return BadRequest(new { message = "Указанный тип узла не существует" });
            }

            var node = new Node
            {
                MapId = createNodeDto.MapId,
                TypeId = nodeType?.Id,
                Title = createNodeDto.Title,
                Description = createNodeDto.Description,
                XPosition = createNodeDto.XPosition,
                YPosition = createNodeDto.YPosition,
                Width = createNodeDto.Width,
                Height = createNodeDto.Height,
                RequiresQuiz = createNodeDto.RequiresQuiz ?? true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Nodes.Add(node);
            await _context.SaveChangesAsync();

            if (node.TypeId.HasValue)
            {
                await SyncNodeFieldValuesAsync(node, createNodeDto.CustomFields);
            }

            var createdNode = await LoadNodeForResponseAsync(node.Id);
            if (createdNode == null)
            {
                return NotFound(new { message = "Узел не найден" });
            }

            return CreatedAtAction(nameof(GetNode), new { id = node.Id }, BuildNodeResponse(createdNode, true, true, false));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNode(int id)
        {
            var userId = GetCurrentUserId();

            var node = await LoadNodeForResponseAsync(id);
            if (node == null)
            {
                return NotFound(new { message = "Узел не найден" });
            }

            var hasAccess = await HasAccessToMap(_context, node.MapId, userId);
            if (!hasAccess)
            {
                return Forbid();
            }

            var accessSnapshot = await MapLearningAccessResolver.BuildAsync(_context, node.MapId, userId);
            var userRole = accessSnapshot.UserRole;
            var isOwner = userRole == "owner";
            var isObserver = userRole == "observer";
            var nodeState = accessSnapshot.NodeStates.TryGetValue(node.Id, out var state)
                ? state
                : new NodeLearningState();

            if (!isOwner && !isObserver && !nodeState.IsVisible)
            {
                return Forbid();
            }

            return Ok(BuildNodeResponse(node, nodeState.CanReadContent, nodeState.IsUnlocked, isOwner));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNode(int id, [FromBody] UpdateNodeDto updateNodeDto)
        {
            var userId = GetCurrentUserId();

            var node = await _context.Nodes
                .Include(n => n.Map)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (node == null)
            {
                return NotFound(new { message = "Узел не найден" });
            }

            if (node.Map.OwnerId != userId)
            {
                return Forbid();
            }

            var previousTypeId = node.TypeId;
            var nodeType = await ResolveNodeTypeAsync(node.MapId, updateNodeDto.TypeId, updateNodeDto.CustomTypeId);
            if (nodeType == null && (updateNodeDto.TypeId.HasValue || updateNodeDto.CustomTypeId.HasValue))
            {
                return BadRequest(new { message = "Указанный тип узла не существует" });
            }

            node.Title = updateNodeDto.Title;
            node.Description = updateNodeDto.Description;
            node.RequiresQuiz = updateNodeDto.RequiresQuiz ?? node.RequiresQuiz;
            node.XPosition = updateNodeDto.XPosition ?? node.XPosition;
            node.YPosition = updateNodeDto.YPosition ?? node.YPosition;
            node.Width = updateNodeDto.Width ?? node.Width;
            node.Height = updateNodeDto.Height ?? node.Height;
            node.TypeId = nodeType?.Id;
            node.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (previousTypeId != node.TypeId || updateNodeDto.CustomFields != null)
            {
                await SyncNodeFieldValuesAsync(node, updateNodeDto.CustomFields ?? new Dictionary<string, object>());
            }

            return Ok(new { message = "Узел успешно обновлён" });
        }

        [HttpPatch("{id}/position")]
        public async Task<IActionResult> UpdateNodePosition(int id, [FromBody] UpdateNodePositionDto positionDto)
        {
            var userId = GetCurrentUserId();

            var node = await _context.Nodes
                .Include(n => n.Map)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (node == null)
            {
                return NotFound(new { message = "Узел не найден" });
            }

            if (node.Map.OwnerId != userId)
            {
                return Forbid();
            }

            node.XPosition = positionDto.XPosition;
            node.YPosition = positionDto.YPosition;
            node.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Позиция узла обновлена" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNode(int id)
        {
            var userId = GetCurrentUserId();

            var node = await _context.Nodes
                .Include(n => n.Map)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (node == null)
            {
                return NotFound(new { message = "Узел не найден" });
            }

            if (node.Map.OwnerId != userId)
            {
                return Forbid();
            }

            var edges = await _context.Edges
                .Where(e => e.SourceNodeId == id || e.TargetNodeId == id)
                .ToListAsync();

            _context.Edges.RemoveRange(edges);
            _context.Nodes.Remove(node);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Узел и связанные с ним связи удалены" });
        }

        [HttpGet("{id}/type")]
        public async Task<IActionResult> GetNodeTypeInfo(int id)
        {
            var userId = GetCurrentUserId();

            var node = await _context.Nodes
                .Include(n => n.Type)
                    .ThenInclude(t => t!.FieldDefinitions)
                        .ThenInclude(f => f.Options)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (node == null)
            {
                return NotFound(new { message = "Узел не найден" });
            }

            var hasAccess = await HasAccessToMap(_context, node.MapId, userId);
            if (!hasAccess)
            {
                return Forbid();
            }

            if (node.Type == null)
            {
                return Ok(new { message = "Тип не установлен" });
            }

            return Ok(new
            {
                node.Type.Id,
                node.Type.Name,
                node.Type.Color,
                node.Type.Icon,
                node.Type.Shape,
                node.Type.Size,
                IsCustom = !node.Type.IsSystem,
                CustomFields = NodeTypeFieldMapper.ToDtos(node.Type.FieldDefinitions)
            });
        }

        private async Task<Node?> LoadNodeForResponseAsync(int nodeId)
        {
            return await _context.Nodes
                .Include(n => n.Type)
                    .ThenInclude(t => t!.FieldDefinitions)
                        .ThenInclude(f => f.Options)
                .Include(n => n.FieldValues)
                    .ThenInclude(v => v.FieldDefinition)
                .Include(n => n.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(n => n.Id == nodeId);
        }

        private async Task<NodeType?> ResolveNodeTypeAsync(int mapId, int? systemTypeId, int? customTypeId)
        {
            if (customTypeId.HasValue)
            {
                return await _context.NodeTypes
                    .FirstOrDefaultAsync(t => t.Id == customTypeId.Value && !t.IsSystem && t.MapId == mapId);
            }

            if (systemTypeId.HasValue)
            {
                return await _context.NodeTypes
                    .FirstOrDefaultAsync(t => t.Id == systemTypeId.Value && t.IsSystem);
            }

            return null;
        }

        private async Task SyncNodeFieldValuesAsync(Node node, Dictionary<string, object>? customFields)
        {
            var existingValues = await _context.NodeFieldValues
                .Where(v => v.NodeId == node.Id)
                .ToListAsync();

            if (!node.TypeId.HasValue)
            {
                _context.NodeFieldValues.RemoveRange(existingValues);
                await _context.SaveChangesAsync();
                return;
            }

            var definitions = await _context.NodeTypeFieldDefinitions
                .Where(f => f.NodeTypeId == node.TypeId.Value)
                .OrderBy(f => f.SortOrder)
                .ToListAsync();

            var definitionIds = definitions.Select(d => d.Id).ToHashSet();
            var staleValues = existingValues
                .Where(v => !definitionIds.Contains(v.NodeTypeFieldDefinitionId))
                .ToList();

            if (staleValues.Count > 0)
            {
                _context.NodeFieldValues.RemoveRange(staleValues);
            }

            var inputValues = customFields ?? new Dictionary<string, object>();

            foreach (var definition in definitions)
            {
                inputValues.TryGetValue(definition.Name, out var rawValue);
                var storageValue = rawValue != null
                    ? NodeTypeFieldMapper.ToStorageString(rawValue)
                    : definition.DefaultValue;

                var existingValue = existingValues.FirstOrDefault(v => v.NodeTypeFieldDefinitionId == definition.Id);
                if (storageValue == null)
                {
                    if (existingValue != null)
                    {
                        _context.NodeFieldValues.Remove(existingValue);
                    }

                    continue;
                }

                if (existingValue != null)
                {
                    existingValue.Value = storageValue;
                }
                else
                {
                    _context.NodeFieldValues.Add(new NodeFieldValue
                    {
                        NodeId = node.Id,
                        NodeTypeFieldDefinitionId = definition.Id,
                        Value = storageValue
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private static object BuildNodeResponse(Node node, bool showContent, bool isUnlocked, bool isOwner)
        {
            var customFields = NodeTypeFieldMapper.ToValueDictionary(node.FieldValues);

            return new
            {
                node.Id,
                node.MapId,
                TypeId = TypeScopeMapper.GetSystemNodeTypeId(node.Type),
                CustomTypeId = TypeScopeMapper.GetCustomNodeTypeId(node.Type),
                TypeName = node.Type?.Name ?? "Неизвестно",
                TypeColor = node.Type?.Color ?? "#3b82f6",
                node.Title,
                Description = showContent ? node.Description : null,
                node.XPosition,
                node.YPosition,
                node.Width,
                node.Height,
                node.RequiresQuiz,
                node.CreatedAt,
                node.UpdatedAt,
                HasQuestions = node.Questions.Any(),
                IsUnlocked = isUnlocked,
                IsVisible = showContent || isOwner,
                CustomFields = showContent ? customFields : null,
                Questions = showContent && node.Questions.Any()
                    ? node.Questions.Select(q => new
                    {
                        q.Id,
                        q.QuestionText,
                        q.QuestionType,
                        AnswerOptions = q.AnswerOptions.Select(a => new
                        {
                            a.Id,
                            a.OptionText,
                            IsCorrect = isOwner ? a.IsCorrect : (bool?)null
                        })
                    })
                    : null
            };
        }
    }
}
