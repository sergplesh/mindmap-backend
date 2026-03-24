using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class NodeFieldValue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NodeId { get; set; }

        [ForeignKey(nameof(NodeId))]
        public virtual Node Node { get; set; } = null!;

        [Required]
        public int NodeTypeFieldDefinitionId { get; set; }

        [ForeignKey(nameof(NodeTypeFieldDefinitionId))]
        public virtual NodeTypeFieldDefinition FieldDefinition { get; set; } = null!;

        [MaxLength(4000)]
        public string? Value { get; set; }
    }
}
