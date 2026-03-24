using System.Globalization;
using System.Text.Json;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;

namespace KnowledgeMap.Backend.Services
{
    public static class NodeTypeFieldMapper
    {
        public static List<CustomFieldDto> ToDtos(IEnumerable<NodeTypeFieldDefinition>? definitions)
        {
            return definitions?
                .OrderBy(d => d.SortOrder)
                .Select(d => new CustomFieldDto
                {
                    Name = d.Name,
                    Type = d.FieldType,
                    Required = d.IsRequired,
                    DefaultValue = d.DefaultValue,
                    Placeholder = d.Placeholder,
                    Validation = d.Validation,
                    Options = d.Options
                        .OrderBy(o => o.SortOrder)
                        .Select(o => o.Value)
                        .ToList()
                })
                .ToList()
                ?? new List<CustomFieldDto>();
        }

        public static Dictionary<string, object>? ToValueDictionary(IEnumerable<NodeFieldValue>? values)
        {
            if (values == null)
            {
                return null;
            }

            var result = values
                .Where(v => v.FieldDefinition != null)
                .OrderBy(v => v.FieldDefinition.SortOrder)
                .ToDictionary(
                    v => v.FieldDefinition.Name,
                    v => ParseStoredValue(v.Value, v.FieldDefinition.FieldType));

            return result.Count == 0 ? null : result;
        }

        public static string? ToStorageString(object? value)
        {
            if (value == null)
            {
                return null;
            }

            return value switch
            {
                string text => text,
                bool boolean => boolean ? "true" : "false",
                JsonElement element => ToStorageString(element),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        public static string? ToStorageString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => element.GetRawText(),
                _ => element.ToString()
            };
        }

        private static object ParseStoredValue(string? value, string fieldType)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return fieldType switch
            {
                "number" when double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number) => number,
                "checkbox" when bool.TryParse(value, out var boolean) => boolean,
                _ => value
            };
        }
    }
}
