using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;

namespace KnowledgeMap.Backend.Services
{
    public interface INodesService
    {
        Task<ServiceResult> CreateNodeAsync(int userId, CreateNodeDto createNodeDto);
        Task<ServiceResult> GetNodeAsync(int nodeId, int userId);
        Task<ServiceResult> UpdateNodeAsync(int nodeId, int userId, UpdateNodeDto updateNodeDto);
        Task<ServiceResult> UpdateNodePositionAsync(int nodeId, int userId, UpdateNodePositionDto positionDto);
        Task<ServiceResult> DeleteNodeAsync(int nodeId, int userId);
        Task<ServiceResult> GetNodeTypeInfoAsync(int nodeId, int userId);
    }

    public class NodesService : INodesService
    {
        private readonly INodesRepository _repository;
        private readonly IMapLearningAccessService _mapLearningAccessService;

        public NodesService(INodesRepository repository, IMapLearningAccessService mapLearningAccessService)
        {
            _repository = repository;
            _mapLearningAccessService = mapLearningAccessService;
        }

        public async Task<ServiceResult> CreateNodeAsync(int userId, CreateNodeDto createNodeDto)
        {
            var map = await _repository.GetMapByIdAsync(createNodeDto.MapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            var nodeType = await _repository.ResolveNodeTypeAsync(createNodeDto.MapId, createNodeDto.TypeId, createNodeDto.CustomTypeId);
            if (nodeType == null && (createNodeDto.TypeId.HasValue || createNodeDto.CustomTypeId.HasValue))
            {
                return ServiceResult.BadRequest(new { message = "Указанный тип узла не существует" });
            }

            var now = DateTime.UtcNow;
            map.UpdatedAt = now;

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
                CreatedAt = now,
                UpdatedAt = now
            };

            await _repository.AddNodeAsync(node);

            if (node.TypeId.HasValue)
            {
                await SyncNodeFieldValuesAsync(node, createNodeDto.CustomFields);
            }

            var createdNode = await _repository.GetNodeForResponseAsync(node.Id);
            if (createdNode == null)
            {
                return ServiceResult.NotFound(new { message = "Узел не найден" });
            }

            return ServiceResult.Created(BuildNodeResponse(createdNode, true, true, false), new { id = node.Id });
        }

        public async Task<ServiceResult> GetNodeAsync(int nodeId, int userId)
        {
            var node = await _repository.GetNodeForResponseAsync(nodeId);
            if (node == null)
            {
                return ServiceResult.NotFound(new { message = "Узел не найден" });
            }

            var hasAccess = await _repository.HasAccessToMapAsync(node.MapId, userId);
            if (!hasAccess)
            {
                return ServiceResult.Forbidden();
            }

            var accessSnapshot = await _mapLearningAccessService.BuildAsync(node.MapId, userId);
            var userRole = accessSnapshot.UserRole;
            var isOwner = userRole == "owner";
            var isObserver = userRole == "observer";
            var nodeState = accessSnapshot.NodeStates.TryGetValue(node.Id, out var state)
                ? state
                : new NodeLearningState();

            if (!isOwner && !isObserver && !nodeState.IsVisible)
            {
                return ServiceResult.Forbidden();
            }

            return ServiceResult.Success(BuildNodeResponse(node, nodeState.CanReadContent, nodeState.IsUnlocked, isOwner));
        }

        public async Task<ServiceResult> UpdateNodeAsync(int nodeId, int userId, UpdateNodeDto updateNodeDto)
        {
            var node = await _repository.GetNodeWithMapAsync(nodeId);
            if (node == null)
            {
                return ServiceResult.NotFound(new { message = "Узел не найден" });
            }

            if (node.Map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            var previousTypeId = node.TypeId;
            var nodeType = await _repository.ResolveNodeTypeAsync(node.MapId, updateNodeDto.TypeId, updateNodeDto.CustomTypeId);
            if (nodeType == null && (updateNodeDto.TypeId.HasValue || updateNodeDto.CustomTypeId.HasValue))
            {
                return ServiceResult.BadRequest(new { message = "Указанный тип узла не существует" });
            }

            node.Title = updateNodeDto.Title;
            node.Description = updateNodeDto.Description;
            node.RequiresQuiz = updateNodeDto.RequiresQuiz ?? node.RequiresQuiz;
            node.XPosition = updateNodeDto.XPosition ?? node.XPosition;
            node.YPosition = updateNodeDto.YPosition ?? node.YPosition;
            node.Width = updateNodeDto.Width ?? node.Width;
            node.Height = updateNodeDto.Height ?? node.Height;
            node.TypeId = nodeType?.Id;
            var now = DateTime.UtcNow;
            node.UpdatedAt = now;
            node.Map.UpdatedAt = now;

            await _repository.SaveChangesAsync();

            if (previousTypeId != node.TypeId || updateNodeDto.CustomFields != null)
            {
                await SyncNodeFieldValuesAsync(node, updateNodeDto.CustomFields ?? new Dictionary<string, object>());
            }

            return ServiceResult.Success(new { message = "Узел успешно обновлён" });
        }

        public async Task<ServiceResult> UpdateNodePositionAsync(int nodeId, int userId, UpdateNodePositionDto positionDto)
        {
            var node = await _repository.GetNodeWithMapAsync(nodeId);
            if (node == null)
            {
                return ServiceResult.NotFound(new { message = "Узел не найден" });
            }

            if (node.Map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            node.XPosition = positionDto.XPosition;
            node.YPosition = positionDto.YPosition;
            var now = DateTime.UtcNow;
            node.UpdatedAt = now;
            node.Map.UpdatedAt = now;

            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Позиция узла обновлена" });
        }

        public async Task<ServiceResult> DeleteNodeAsync(int nodeId, int userId)
        {
            var node = await _repository.GetNodeWithMapAsync(nodeId);
            if (node == null)
            {
                return ServiceResult.NotFound(new { message = "Узел не найден" });
            }

            if (node.Map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            var edges = await _repository.GetEdgesForNodeAsync(nodeId);
            node.Map.UpdatedAt = DateTime.UtcNow;
            _repository.RemoveEdges(edges);
            _repository.RemoveNode(node);
            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Узел и связанные с ним связи удалены" });
        }

        public async Task<ServiceResult> GetNodeTypeInfoAsync(int nodeId, int userId)
        {
            var node = await _repository.GetNodeWithTypeInfoAsync(nodeId);
            if (node == null)
            {
                return ServiceResult.NotFound(new { message = "Узел не найден" });
            }

            var hasAccess = await _repository.HasAccessToMapAsync(node.MapId, userId);
            if (!hasAccess)
            {
                return ServiceResult.Forbidden();
            }

            if (node.Type == null)
            {
                return ServiceResult.Success(new { message = "Тип не установлен" });
            }

            return ServiceResult.Success(new
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

        private async Task SyncNodeFieldValuesAsync(Node node, Dictionary<string, object>? customFields)
        {
            var existingValues = await _repository.GetNodeFieldValuesAsync(node.Id);

            if (!node.TypeId.HasValue)
            {
                _repository.RemoveNodeFieldValues(existingValues);
                await _repository.SaveChangesAsync();
                return;
            }

            var definitions = await _repository.GetNodeTypeFieldDefinitionsAsync(node.TypeId.Value);
            var definitionIds = definitions.Select(d => d.Id).ToHashSet();
            var staleValues = existingValues
                .Where(v => !definitionIds.Contains(v.NodeTypeFieldDefinitionId))
                .ToList();

            if (staleValues.Count > 0)
            {
                _repository.RemoveNodeFieldValues(staleValues);
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
                        _repository.RemoveNodeFieldValues(new[] { existingValue });
                    }

                    continue;
                }

                if (existingValue != null)
                {
                    existingValue.Value = storageValue;
                }
                else
                {
                    _repository.AddNodeFieldValue(new NodeFieldValue
                    {
                        NodeId = node.Id,
                        NodeTypeFieldDefinitionId = definition.Id,
                        Value = storageValue
                    });
                }
            }

            await _repository.SaveChangesAsync();
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
