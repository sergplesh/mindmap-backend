using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class AnswerOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; } = null!;

        [Required]
        public string OptionText { get; set; } = string.Empty;

        [Required]
        public bool IsCorrect { get; set; }
    }
}