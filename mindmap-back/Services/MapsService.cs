using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;

namespace KnowledgeMap.Backend.Services
{
    public interface IMapsService
    {
        Task<ServiceResult> GetMyMapsAsync(int userId);
        Task<ServiceResult> GetMapAsync(int mapId, int userId);
        Task<ServiceResult> CreateMapAsync(int userId, CreateMapDto createMapDto);
        Task<ServiceResult> UpdateMapAsync(int mapId, int userId, UpdateMapDto updateMapDto);
        Task<ServiceResult> DeleteMapAsync(int mapId, int userId);
        Task<ServiceResult> GetMapNodesAsync(int mapId, int userId);
        Task<ServiceResult> GetMapEdgesAsync(int mapId, int userId);
        Task<ServiceResult> GetFullMapAsync(int mapId, int userId);
    }

    public class MapsService : IMapsService
    {
        private readonly IMapsRepository _repository;
        private readonly IMapLearningAccessService _mapLearningAccessService;

        public MapsService(IMapsRepository repository, IMapLearningAccessService mapLearningAccessService)
        {
            _repository = repository;
            _mapLearningAccessService = mapLearningAccessService;
        }

        public async Task<ServiceResult> GetMyMapsAsync(int userId)
        {
            var maps = await _repository.GetAccessibleMapSummariesAsync(userId);
            var result = maps.Select(m => new MapDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                Emoji = m.Emoji,
                OwnerId = m.OwnerId,
                OwnerName = m.OwnerName,
                UserRole = m.UserRole,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                NodesCount = m.NodesCount,
                EdgesCount = m.EdgesCount
            }).ToList();

            return ServiceResult.Success(result);
        }

        public async Task<ServiceResult> GetMapAsync(int mapId, int userId)
        {
            var map = await _repository.GetMapDetailsAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (!await _repository.HasAccessToMapAsync(mapId, userId))
            {
                return ServiceResult.Forbidden();
            }

            var edges = await _repository.GetEdgesForMapAsync(mapId);
            var userRole = await GetUserRoleAsync(map, userId);

            return ServiceResult.Success(new
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

        public async Task<ServiceResult> CreateMapAsync(int userId, CreateMapDto createMapDto)
        {
            var map = new Map
            {
                Title = createMapDto.Title,
                Description = createMapDto.Description,
                Emoji = createMapDto.Emoji,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddMapAsync(map);

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

            await _repository.AddNodeAsync(centralNode);

            return ServiceResult.Created(new
            {
                map.Id,
                map.Title,
                map.Description,
                map.Emoji,
                map.OwnerId,
                map.CreatedAt,
                map.UpdatedAt
            }, new { id = map.Id });
        }

        public async Task<ServiceResult> UpdateMapAsync(int mapId, int userId, UpdateMapDto updateMapDto)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            map.Title = updateMapDto.Title;
            map.Description = updateMapDto.Description;
            map.Emoji = updateMapDto.Emoji;
            map.UpdatedAt = DateTime.UtcNow;

            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new
            {
                map.Id,
                map.Title,
                map.Description,
                map.Emoji,
                map.UpdatedAt
            });
        }

        public async Task<ServiceResult> DeleteMapAsync(int mapId, int userId)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != userId)
            {
                return ServiceResult.Forbidden();
            }

            var edges = await _repository.GetEdgesForMapAsync(mapId);
            _repository.RemoveEdges(edges);
            _repository.RemoveMap(map);
            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Карта успешно удалена" });
        }

        public async Task<ServiceResult> GetMapNodesAsync(int mapId, int userId)
        {
            if (!await _repository.HasAccessToMapAsync(mapId, userId))
            {
                return ServiceResult.Forbidden();
            }

            var nodes = await _repository.GetNodesForMapAsync(mapId);
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

            return ServiceResult.Success(response);
        }

        public async Task<ServiceResult> GetMapEdgesAsync(int mapId, int userId)
        {
            if (!await _repository.HasAccessToMapAsync(mapId, userId))
            {
                return ServiceResult.Forbidden();
            }

            var edges = await _repository.GetEdgesForMapAsync(mapId);
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

            return ServiceResult.Success(response);
        }

        public async Task<ServiceResult> GetFullMapAsync(int mapId, int userId)
        {
            if (!await _repository.HasAccessToMapAsync(mapId, userId))
            {
                return ServiceResult.Forbidden();
            }

            var map = await _repository.GetMapDetailsAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            var edges = await _repository.GetEdgesForMapAsync(mapId);
            var accessSnapshot = await _mapLearningAccessService.BuildAsync(mapId, userId, map);
            var userRole = accessSnapshot.UserRole;

            return ServiceResult.Success(new
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
                        Label = string.IsNullOrWhiteSpace(e.Label) ? (e.Type != null ? e.Type.Label : string.Empty) : e.Label,
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

        private async Task<string> GetUserRoleAsync(Map map, int userId)
        {
            if (map.OwnerId == userId)
            {
                return "owner";
            }

            var access = await _repository.GetAccessRoleAsync(map.Id, userId);
            return access ?? "observer";
        }
    }
}
