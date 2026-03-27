using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class AnswerResultQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnswerResultId { get; set; }

        [ForeignKey(nameof(AnswerResultId))]
        public virtual AnswerResult AnswerResult { get; set; } = null!;

        public int? QuestionId { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string QuestionType { get; set; } = "single_choice";

        public int DisplayOrder { get; set; }

        public virtual ICollection<AnswerResultQuestionOption> Options { get; set; } = new List<AnswerResultQuestionOption>();
    }
}
