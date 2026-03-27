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
                return ServiceResult.NotFound(new { message = "Р С™Р В°РЎР‚РЎвЂљР В° Р Р…Р Вµ Р Р…Р В°Р в„–Р Т‘Р ВµР Р…Р В°" });
            }

            if (map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            var sourceNode = await _repository.GetNodeByIdAsync(createEdgeDto.SourceNodeId);
            var targetNode = await _repository.GetNodeByIdAsync(createEdgeDto.TargetNodeId);

            if (sourceNode == null || targetNode == null)
            {
                return ServiceResult.BadRequest(new { message = "Р С›Р Т‘Р С‘Р Р… Р С‘Р В· РЎС“Р В·Р В»Р С•Р Р† Р Р…Р Вµ РЎРѓРЎС“РЎвЂ°Р ВµРЎРѓРЎвЂљР Р†РЎС“Р ВµРЎвЂљ" });
            }

            if (sourceNode.MapId != createEdgeDto.MapId || targetNode.MapId != createEdgeDto.MapId)
            {
                return ServiceResult.BadRequest(new { message = "Р Р€Р В·Р В»РЎвЂ№ Р Т‘Р С•Р В»Р В¶Р Р…РЎвЂ№ Р С—РЎР‚Р С‘Р Р…Р В°Р Т‘Р В»Р ВµР В¶Р В°РЎвЂљРЎРЉ РЎС“Р С”Р В°Р В·Р В°Р Р…Р Р…Р С•Р в„– Р С”Р В°РЎР‚РЎвЂљР Вµ" });
            }

            var existingEdge = await _repository.GetExistingEdgeAsync(createEdgeDto.SourceNodeId, createEdgeDto.TargetNodeId);
            if (existingEdge != null)
            {
                return ServiceResult.BadRequest(new { message = "Р СћР В°Р С”Р В°РЎРЏ РЎРѓР Р†РЎРЏР В·РЎРЉ РЎС“Р В¶Р Вµ РЎРѓРЎС“РЎвЂ°Р ВµРЎРѓРЎвЂљР Р†РЎС“Р ВµРЎвЂљ" });
            }

            var edgeType = await _repository.ResolveEdgeTypeAsync(createEdgeDto.MapId, createEdgeDto.TypeId, createEdgeDto.CustomTypeId);
            if (edgeType == null && (createEdgeDto.TypeId.HasValue || createEdgeDto.CustomTypeId.HasValue))
            {
                return ServiceResult.BadRequest(new { message = "Р Р€Р С”Р В°Р В·Р В°Р Р…Р Р…РЎвЂ№Р в„– РЎвЂљР С‘Р С— РЎРѓР Р†РЎРЏР В·Р С‘ Р Р…Р Вµ РЎРѓРЎС“РЎвЂ°Р ВµРЎРѓРЎвЂљР Р†РЎС“Р ВµРЎвЂљ" });
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

            await _repository.AddEdgeAsync(edge);

            var createdEdge = await _repository.GetEdgeForResponseAsync(edge.Id);
            if (createdEdge == null)
            {
                return ServiceResult.NotFound(new { message = "Р РЋР Р†РЎРЏР В·РЎРЉ Р Р…Р Вµ Р Р…Р В°Р в„–Р Т‘Р ВµР Р…Р В°" });
            }

            return ServiceResult.Created(BuildEdgeResponse(createdEdge), new { id = edge.Id });
        }

        public async Task<ServiceResult> GetEdgeAsync(int edgeId, int userId)
        {
            var edge = await _repository.GetEdgeForResponseAsync(edgeId);
            if (edge == null)
            {
                return ServiceResult.NotFound(new { message = "Р РЋР Р†РЎРЏР В·РЎРЉ Р Р…Р Вµ Р Р…Р В°Р в„–Р Т‘Р ВµР Р…Р В°" });
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
                return ServiceResult.NotFound(new { message = "Р РЋР Р†РЎРЏР В·РЎРЉ Р Р…Р Вµ Р Р…Р В°Р в„–Р Т‘Р ВµР Р…Р В°" });
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
                    return ServiceResult.BadRequest(new { message = "РЈРєР°Р·Р°РЅРЅС‹Р№ С‚РёРї СЃРІСЏР·Рё РЅРµ СЃСѓС‰РµСЃС‚РІСѓРµС‚" });
                }

                edge.TypeId = edgeType.Id;
            }

            edge.Label = NormalizeLabel(updateEdgeDto.Label);
            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Р РЋР Р†РЎРЏР В·РЎРЉ РЎС“РЎРѓР С—Р ВµРЎв‚¬Р Р…Р С• Р С•Р В±Р Р…Р С•Р Р†Р В»Р ВµР Р…Р В°" });
        }

        public async Task<ServiceResult> DeleteEdgeAsync(int edgeId, int userId)
        {
            var edge = await _repository.GetEdgeWithOwnerAsync(edgeId);
            if (edge == null)
            {
                return ServiceResult.NotFound(new { message = "Р РЋР Р†РЎРЏР В·РЎРЉ Р Р…Р Вµ Р Р…Р В°Р в„–Р Т‘Р ВµР Р…Р В°" });
            }

            if (edge.SourceNode.Map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            _repository.RemoveEdge(edge);
            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Р РЋР Р†РЎРЏР В·РЎРЉ РЎС“РЎРѓР С—Р ВµРЎв‚¬Р Р…Р С• РЎС“Р Т‘Р В°Р В»Р ВµР Р…Р В°" });
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
                TypeName = edge.Type?.Name ?? "Р СњР ВµР С‘Р В·Р Р†Р ВµРЎРѓРЎвЂљР Р…Р С•",
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
