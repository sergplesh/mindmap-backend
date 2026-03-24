using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class Node
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MapId { get; set; }

        [ForeignKey(nameof(MapId))]
        public virtual Map Map { get; set; } = null!;

        public int? TypeId { get; set; }

        [ForeignKey(nameof(TypeId))]
        public virtual NodeType? Type { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public double XPosition { get; set; }

        [Required]
        public double YPosition { get; set; }

        public double Width { get; set; } = 200;
        public double Height { get; set; } = 80;

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        public bool RequiresQuiz { get; set; } = true;

        [NotMapped]
        public int Level { get; set; }

        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<Edge> SourceEdges { get; set; } = new List<Edge>();
        public virtual ICollection<Edge> TargetEdges { get; set; } = new List<Edge>();
        public virtual ICollection<NodeFieldValue> FieldValues { get; set; } = new List<NodeFieldValue>();
        public virtual ICollection<LearningProgress> LearningProgresses { get; set; } = new List<LearningProgress>();
        public virtual ICollection<AnswerResult> AnswerResults { get; set; } = new List<AnswerResult>();
    }
}
