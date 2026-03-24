using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class LearningProgress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [Required]
        public int NodeId { get; set; }

        [ForeignKey("NodeId")]
        public virtual Node Node { get; set; } = null!;

        [Range(0, 100)]
        public int MasteryLevel { get; set; }

        public string? PersonalNotes { get; set; }
    }
}