using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;

namespace KnowledgeMap.Backend.Services
{
    public interface IEdgesService
    {
        Task<ServiceResult> CreateEdgeAsync(int userId, CreateEdgeDto createEdgeDto);
        Task<ServiceResult> GetEdgeAsync(int edgeId, int userId);
        Task<ServiceResult> UpdateEdgeAsync(int edgeId, int userId, UpdateEdgeDto updateEdgeDto);
        Task<ServiceResult> DeleteEdgeAsync(int edgeId, int userId);
    }

    public class EdgesService : IEdgesService
    {
        private readonly IEdgesRepository _repository;

        public EdgesService(IEdgesRepository repository)
        {
            _repository = repository;
        }

        public async Task<ServiceResult> CreateEdgeAsync(int userId, CreateEdgeDto createEdgeDto)
        {
            var map = await _repository.GetMapByIdAsync(createEdgeDto.MapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            var sourceNode = await _repository.GetNodeByIdAsync(createEdgeDto.SourceNodeId);
            var targetNode = await _repository.GetNodeByIdAsync(createEdgeDto.TargetNodeId);

            if (sourceNode == null || targetNode == null)
            {
                return ServiceResult.BadRequest(new { message = "Один из узлов не существует" });
            }

            if (sourceNode.MapId != createEdgeDto.MapId || targetNode.MapId != createEdgeDto.MapId)
            {
                return ServiceResult.BadRequest(new { message = "Узлы должны принадлежать указанной карте" });
            }

            var existingEdge = await _repository.GetExistingEdgeAsync(createEdgeDto.SourceNodeId, createEdgeDto.TargetNodeId);
            if (existingEdge != null)
            {
                return ServiceResult.BadRequest(new { message = "Такая связь уже существует" });
            }

            var edgeType = await _repository.ResolveEdgeTypeAsync(createEdgeDto.MapId, createEdgeDto.TypeId, createEdgeDto.CustomTypeId);
            if (edgeType == null && (createEdgeDto.TypeId.HasValue || createEdgeDto.CustomTypeId.HasValue))
            {
                return ServiceResult.BadRequest(new { message = "Указанный тип связи не существует" });
            }

            var now = DateTime.UtcNow;
            map.UpdatedAt = now;

            var edge = new Edge
            {
                SourceNodeId = createEdgeDto.SourceNodeId,
                TargetNodeId = createEdgeDto.TargetNodeId,
                TypeId = edgeType?.Id,
                Label = NormalizeLabel(createEdgeDto.Label),
                IsHierarchy = createEdgeDto.IsHierarchy,
                CreatedAt = now
            };

            await _repository.AddEdgeAsync(edge);

            var createdEdge = await _repository.GetEdgeForResponseAsync(edge.Id);
            if (createdEdge == null)
            {
                return ServiceResult.NotFound(new { message = "Связь не найдена" });
            }

            return ServiceResult.Created(BuildEdgeResponse(createdEdge), new { id = edge.Id });
        }

        public async Task<ServiceResult> GetEdgeAsync(int edgeId, int userId)
        {
            var edge = await _repository.GetEdgeForResponseAsync(edgeId);
            if (edge == null)
            {
                return ServiceResult.NotFound(new { message = "Связь не найдена" });
            }

            var hasAccess = await _repository.HasAccessToMapAsync(edge.SourceNode.MapId, userId);
            if (!hasAccess)
            {
                return ServiceResult.Forbidden();
            }

            return ServiceResult.Success(BuildEdgeResponse(edge));
        }

        public async Task<ServiceResult> UpdateEdgeAsync(int edgeId, int userId, UpdateEdgeDto updateEdgeDto)
        {
            var edge = await _repository.GetEdgeWithOwnerAsync(edgeId);
            if (edge == null)
            {
                return ServiceResult.NotFound(new { message = "Связь не найдена" });
            }

            if (edge.SourceNode.Map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            if (updateEdgeDto.TypeId.HasValue || updateEdgeDto.CustomTypeId.HasValue)
            {
                var edgeType = await _repository.ResolveEdgeTypeAsync(edge.SourceNode.MapId, updateEdgeDto.TypeId, updateEdgeDto.CustomTypeId);
                if (edgeType == null)
                {
                    return ServiceResult.BadRequest(new { message = "Указанный тип связи не существует" });
                }

                edge.TypeId = edgeType.Id;
            }

            edge.Label = NormalizeLabel(updateEdgeDto.Label);
            edge.SourceNode.Map.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Связь успешно обновлена" });
        }

        public async Task<ServiceResult> DeleteEdgeAsync(int edgeId, int userId)
        {
            var edge = await _repository.GetEdgeWithOwnerAsync(edgeId);
            if (edge == null)
            {
                return ServiceResult.NotFound(new { message = "Связь не найдена" });
            }

            if (edge.SourceNode.Map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            edge.SourceNode.Map.UpdatedAt = DateTime.UtcNow;
            _repository.RemoveEdge(edge);
            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Связь успешно удалена" });
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
                TypeName = edge.Type?.Name ?? "Неизвестно",
                Label = string.IsNullOrWhiteSpace(edge.Label) ? (edge.Type?.Label ?? string.Empty) : edge.Label,
                TypeStyle = edge.Type?.Style ?? "solid",
                TypeLabel = edge.Type?.Label ?? string.Empty,
                TypeColor = edge.Type?.Color ?? "#666666",
                TypeIsBidirectional = edge.Type?.IsBidirectional ?? false,
                edge.IsHierarchy,
                edge.CreatedAt
            };
        }
    }
}
