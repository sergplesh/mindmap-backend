using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class EdgeType
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
        public string Style { get; set; } = "solid";

        [MaxLength(50)]
        public string? Label { get; set; }

        [Required]
        [MaxLength(20)]
        public string Color { get; set; } = "#666666";

        [Required]
        public bool IsSystem { get; set; }

        public virtual ICollection<Edge> Edges { get; set; } = new List<Edge>();
    }
}
