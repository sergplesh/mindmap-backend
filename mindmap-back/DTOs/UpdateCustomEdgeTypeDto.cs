using System.ComponentModel.DataAnnotations;

namespace KnowledgeMap.Backend.DTOs
{
    public class UpdateCustomEdgeTypeDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Style { get; set; } = "solid";

        [MaxLength(50)]
        public string? Label { get; set; }

        [MaxLength(20)]
        public string Color { get; set; } = "#666666";
    }
}
