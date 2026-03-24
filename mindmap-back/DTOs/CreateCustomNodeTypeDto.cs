using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KnowledgeMap.Backend.DTOs
{
    public class CreateCustomNodeTypeDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Color { get; set; } = "#3b82f6";

        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(20)]
        public string Shape { get; set; } = "rect";

        [MaxLength(20)]
        public string Size { get; set; } = "medium";

        public List<CustomFieldDto>? CustomFields { get; set; }
    }

    public class CustomFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public bool Required { get; set; }
        public string? DefaultValue { get; set; }
        public string? Placeholder { get; set; }
        public string? Validation { get; set; }
        public List<string>? Options { get; set; }
    }
    public class UpdateCustomNodeTypeDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Color { get; set; } = "#3b82f6";

        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(20)]
        public string Shape { get; set; } = "rect";

        [MaxLength(20)]
        public string Size { get; set; } = "medium";

        public List<CustomFieldDto>? CustomFields { get; set; }
    }
}