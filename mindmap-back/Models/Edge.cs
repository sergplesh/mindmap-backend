using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class Edge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SourceNodeId { get; set; }

        [ForeignKey(nameof(SourceNodeId))]
        public virtual Node SourceNode { get; set; } = null!;

        [Required]
        public int TargetNodeId { get; set; }

        [ForeignKey(nameof(TargetNodeId))]
        public virtual Node TargetNode { get; set; } = null!;

        public int? TypeId { get; set; }

        [ForeignKey(nameof(TypeId))]
        public virtual EdgeType? Type { get; set; }

        [MaxLength(255)]
        public string? Label { get; set; }

        public bool IsHierarchy { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
