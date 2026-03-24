using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NodeId { get; set; }

        [ForeignKey("NodeId")]
        public virtual Node Node { get; set; } = null!;

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string QuestionType { get; set; } = "single_choice";

        public virtual ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
    }
}