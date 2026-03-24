using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class NodeType
    {
        [Key]
        public int Id { get; set; }

        public int? MapId { get; set; }

        [ForeignKey(nameof(MapId))]
        public virtual Map? Map { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Color { get; set; } = "#3b82f6";

        [MaxLength(50)]
        public string? Icon { get; set; }

        [Required]
        [MaxLength(20)]
        public string Shape { get; set; } = "rect";

        [Required]
        [MaxLength(20)]
        public string Size { get; set; } = "medium";

        [Required]
        public bool IsSystem { get; set; }

        public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();
        public virtual ICollection<NodeTypeFieldDefinition> FieldDefinitions { get; set; } = new List<NodeTypeFieldDefinition>();
    }
}
