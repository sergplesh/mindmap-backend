using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class AnswerResultQuestionOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnswerResultQuestionId { get; set; }

        [ForeignKey(nameof(AnswerResultQuestionId))]
        public virtual AnswerResultQuestion Question { get; set; } = null!;

        public int? AnswerOptionId { get; set; }

        [Required]
        public string OptionText { get; set; } = string.Empty;

        [Required]
        public bool IsCorrect { get; set; }

        [Required]
        public bool IsSelected { get; set; }

        public int DisplayOrder { get; set; }
    }
}
