using System.ComponentModel.DataAnnotations;

namespace KnowledgeMap.Backend.DTOs
{
    public class CreateNodeDto
    {
        [Required]
        public int MapId { get; set; }

        public int? TypeId { get; set; }
        public int? CustomTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public double XPosition { get; set; } = 0;
        public double YPosition { get; set; } = 0;

        public double Width { get; set; } = 200;
        public double Height { get; set; } = 80;

        public bool? RequiresQuiz { get; set; } = true;

        public Dictionary<string, object>? CustomFields { get; set; }
    }
}