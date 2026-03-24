using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class AnswerResultSelection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnswerResultId { get; set; }

        [ForeignKey(nameof(AnswerResultId))]
        public virtual AnswerResult AnswerResult { get; set; } = null!;

        [Required]
        public int AnswerOptionId { get; set; }

        [ForeignKey(nameof(AnswerOptionId))]
        public virtual AnswerOption AnswerOption { get; set; } = null!;
    }
}
