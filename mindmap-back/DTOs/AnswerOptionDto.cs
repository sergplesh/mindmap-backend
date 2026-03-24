using System.ComponentModel.DataAnnotations;

namespace KnowledgeMap.Backend.DTOs
{
    public class AnswerOptionDto
    {
        public int? Id { get; set; }

        [Required]
        public string OptionText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}
