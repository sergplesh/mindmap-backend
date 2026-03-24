using System.ComponentModel.DataAnnotations;

namespace KnowledgeMap.Backend.DTOs
{
    public class UpdateNodeDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? TypeId { get; set; }
        public int? CustomTypeId { get; set; }

        public bool? RequiresQuiz { get; set; }

        public double? XPosition { get; set; }
        public double? YPosition { get; set; }

        public double? Width { get; set; }
        public double? Height { get; set; }

        public Dictionary<string, object>? CustomFields { get; set; }
    }
}
