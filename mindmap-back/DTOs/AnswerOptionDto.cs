using System.ComponentModel.DataAnnotations;

namespace KnowledgeMap.Backend.DTOs
{
    public class AnswerOptionDto
    {
        [Required]
        public string OptionText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }
}
