using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class AnswerResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        [Required]
        public int NodeId { get; set; }

        [ForeignKey(nameof(NodeId))]
        public virtual Node Node { get; set; } = null!;

        [Required]
        public bool IsPassed { get; set; }

        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<AnswerResultSelection> Selections { get; set; } = new List<AnswerResultSelection>();
    }
}
