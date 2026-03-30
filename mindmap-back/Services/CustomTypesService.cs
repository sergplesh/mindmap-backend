using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;

namespace KnowledgeMap.Backend.Services
{
    public interface ICustomTypesService
    {
        Task<ServiceResult> GetCustomNodeTypesAsync(int mapId, int currentUserId);
        Task<ServiceResult> GetCustomNodeTypeAsync(int mapId, int typeId, int currentUserId);
        Task<ServiceResult> GetCustomEdgeTypesAsync(int mapId, int currentUserId);
        Task<ServiceResult> GetCustomEdgeTypeAsync(int mapId, int typeId, int currentUserId);
        Task<ServiceResult> CreateCustomNodeTypeAsync(int mapId, int currentUserId, CreateCustomNodeTypeDto dto);
        Task<ServiceResult> CreateCustomEdgeTypeAsync(int mapId, int currentUserId, CreateCustomEdgeTypeDto dto);
        Task<ServiceResult> UpdateCustomNodeTypeAsync(int mapId, int typeId, int currentUserId, UpdateCustomNodeTypeDto dto);
        Task<ServiceResult> UpdateCustomEdgeTypeAsync(int mapId, int typeId, int currentUserId, UpdateCustomEdgeTypeDto dto);
        Task<ServiceResult> DeleteCustomNodeTypeAsync(int mapId, int typeId, int currentUserId);
        Task<ServiceResult> DeleteCustomEdgeTypeAsync(int mapId, int typeId, int currentUserId);
    }

    public class CustomTypesService : ICustomTypesService
    {
        private readonly ICustomTypesRepository _repository;

        public CustomTypesService(ICustomTypesRepository repository)
        {
            _repository = repository;
        }

        public async Task<ServiceResult> GetCustomNodeTypesAsync(int mapId, int currentUserId)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (!await _repository.HasMapAccessAsync(mapId, currentUserId))
            {
                return ServiceResult.Forbidden();
            }

            var systemTypes = await _repository.GetSystemNodeTypesAsync();
            var customTypes = await _repository.GetCustomNodeTypesAsync(mapId);

            return ServiceResult.Success(new
            {
                system = systemTypes.Select(ToNodeTypeResponse),
                custom = customTypes.Select(ToNodeTypeResponse)
            });
        }

        public async Task<ServiceResult> GetCustomNodeTypeAsync(int mapId, int typeId, int currentUserId)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (!await _repository.HasMapAccessAsync(mapId, currentUserId))
            {
                return ServiceResult.Forbidden();
            }

            var customType = await _repository.GetCustomNodeTypeAsync(mapId, typeId);
            if (customType == null)
            {
                return ServiceResult.NotFound(new { message = "Тип не найден" });
            }

            return ServiceResult.Success(ToNodeTypeResponse(customType));
        }

        public async Task<ServiceResult> GetCustomEdgeTypesAsync(int mapId, int currentUserId)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (!await _repository.HasMapAccessAsync(mapId, currentUserId))
            {
                return ServiceResult.Forbidden();
            }

            var customTypes = await _repository.GetCustomEdgeTypesAsync(mapId);

            return ServiceResult.Success(new
            {
                system = Array.Empty<object>(),
                custom = customTypes.Select(ToEdgeTypeResponse)
            });
        }

        public async Task<ServiceResult> GetCustomEdgeTypeAsync(int mapId, int typeId, int currentUserId)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (!await _repository.HasMapAccessAsync(mapId, currentUserId))
            {
                return ServiceResult.Forbidden();
            }

            var customType = await _repository.GetCustomEdgeTypeAsync(mapId, typeId);
            if (customType == null)
            {
                return ServiceResult.NotFound(new { message = "Тип не найден" });
            }

            return ServiceResult.Success(ToEdgeTypeResponse(customType));
        }

        public async Task<ServiceResult> CreateCustomNodeTypeAsync(int mapId, int currentUserId, CreateCustomNodeTypeDto dto)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != currentUserId)
            {
                return ServiceResult.Forbidden();
            }

            var customType = new NodeType
            {
                MapId = mapId,
                Name = dto.Name,
                Color = dto.Color,
                Icon = string.IsNullOrWhiteSpace(dto.Icon) ? null : dto.Icon,
                Shape = string.IsNullOrWhiteSpace(dto.Shape) ? "rect" : dto.Shape,
                Size = string.IsNullOrWhiteSpace(dto.Size) ? "medium" : dto.Size,
                IsSystem = false
            };

            await _repository.AddNodeTypeAsync(customType);
            await ReplaceNodeTypeFieldDefinitionsAsync(customType, dto.CustomFields);
            await _repository.LoadNodeTypeFieldDefinitionsAsync(customType);

            return ServiceResult.Success(ToNodeTypeResponse(customType));
        }

        public async Task<ServiceResult> CreateCustomEdgeTypeAsync(int mapId, int currentUserId, CreateCustomEdgeTypeDto dto)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != currentUserId)
            {
                return ServiceResult.Forbidden();
            }

            var customType = new EdgeType
            {
                MapId = mapId,
                Name = dto.Name,
                Style = dto.Style,
                Label = dto.Label,
                Color = dto.Color,
                IsBidirectional = dto.IsBidirectional,
                IsSystem = false
            };

            await _repository.AddEdgeTypeAsync(customType);

            return ServiceResult.Success(ToEdgeTypeResponse(customType));
        }

        public async Task<ServiceResult> UpdateCustomNodeTypeAsync(int mapId, int typeId, int currentUserId, UpdateCustomNodeTypeDto dto)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != currentUserId)
            {
                return ServiceResult.Forbidden();
            }

            var customType = await _repository.GetCustomNodeTypeAsync(mapId, typeId);
            if (customType == null)
            {
                return ServiceResult.NotFound(new { message = "Тип не найден" });
            }

            customType.Name = dto.Name;
            customType.Color = dto.Color;
            customType.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? null : dto.Icon;
            customType.Shape = string.IsNullOrWhiteSpace(dto.Shape) ? "rect" : dto.Shape;
            customType.Size = string.IsNullOrWhiteSpace(dto.Size) ? "medium" : dto.Size;

            await ReplaceNodeTypeFieldDefinitionsAsync(customType, dto.CustomFields);
            await _repository.LoadNodeTypeFieldDefinitionsAsync(customType);

            return ServiceResult.Success(ToNodeTypeResponse(customType));
        }

        public async Task<ServiceResult> UpdateCustomEdgeTypeAsync(int mapId, int typeId, int currentUserId, UpdateCustomEdgeTypeDto dto)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != currentUserId)
            {
                return ServiceResult.Forbidden();
            }

            var customType = await _repository.GetCustomEdgeTypeAsync(mapId, typeId);
            if (customType == null)
            {
                return ServiceResult.NotFound(new { message = "Тип не найден" });
            }

            customType.Name = dto.Name;
            customType.Style = dto.Style;
            customType.Label = dto.Label;
            customType.Color = dto.Color;
            customType.IsBidirectional = dto.IsBidirectional;

            await _repository.SaveChangesAsync();

            return ServiceResult.Success(ToEdgeTypeResponse(customType));
        }

        public async Task<ServiceResult> DeleteCustomNodeTypeAsync(int mapId, int typeId, int currentUserId)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != currentUserId)
            {
                return ServiceResult.Forbidden();
            }

            var customType = await _repository.GetCustomNodeTypeAsync(mapId, typeId);
            if (customType == null)
            {
                return ServiceResult.NotFound(new { message = "Тип не найден" });
            }

            if (await _repository.IsNodeTypeUsedAsync(typeId))
            {
                return ServiceResult.BadRequest(new { message = "Нельзя удалить тип, который используется узлами" });
            }

            var definitionIds = customType.FieldDefinitions.Select(f => f.Id).ToList();
            if (definitionIds.Count > 0)
            {
                var fieldValues = await _repository.GetNodeFieldValuesByDefinitionIdsAsync(definitionIds);
                _repository.RemoveNodeFieldValues(fieldValues);
                _repository.RemoveNodeTypeOptions(customType.FieldDefinitions.SelectMany(f => f.Options));
                _repository.RemoveNodeTypeDefinitions(customType.FieldDefinitions);
            }

            _repository.RemoveNodeType(customType);
            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Тип удалён" });
        }

        public async Task<ServiceResult> DeleteCustomEdgeTypeAsync(int mapId, int typeId, int currentUserId)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != currentUserId)
            {
                return ServiceResult.Forbidden();
            }

            var customType = await _repository.GetCustomEdgeTypeAsync(mapId, typeId);
            if (customType == null)
            {
                return ServiceResult.NotFound(new { message = "Тип не найден" });
            }

            if (await _repository.IsEdgeTypeUsedAsync(typeId))
            {
                return ServiceResult.BadRequest(new { message = "Нельзя удалить тип, который используется связями" });
            }

            _repository.RemoveEdgeType(customType);
            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Тип удалён" });
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
                var fieldValues = await _repository.GetNodeFieldValuesByDefinitionIdsAsync(definitionIds);

                _repository.RemoveNodeFieldValues(fieldValues);
                _repository.RemoveNodeTypeOptions(definitionsToRemove.SelectMany(d => d.Options));
                _repository.RemoveNodeTypeDefinitions(definitionsToRemove);
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

                _repository.RemoveNodeTypeOptions(definition.Options.ToList());
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

            await _repository.SaveChangesAsync();
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
                type.IsBidirectional,
                type.IsSystem
            };
        }
    }
}
