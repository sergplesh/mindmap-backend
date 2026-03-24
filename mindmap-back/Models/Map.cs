using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class Map
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(10)]
        public string? Emoji { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [ForeignKey(nameof(OwnerId))]
        public virtual User Owner { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();
        public virtual ICollection<NodeType> NodeTypes { get; set; } = new List<NodeType>();
        public virtual ICollection<EdgeType> EdgeTypes { get; set; } = new List<EdgeType>();
        public virtual ICollection<Access> Accesses { get; set; } = new List<Access>();
    }
}
