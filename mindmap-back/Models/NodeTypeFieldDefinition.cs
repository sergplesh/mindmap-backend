using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class NodeTypeFieldDefinition
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NodeTypeId { get; set; }

        [ForeignKey(nameof(NodeTypeId))]
        public virtual NodeType NodeType { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string FieldType { get; set; } = "text";

        [Required]
        public bool IsRequired { get; set; }

        [MaxLength(2000)]
        public string? DefaultValue { get; set; }

        [MaxLength(2000)]
        public string? Placeholder { get; set; }

        [MaxLength(500)]
        public string? Validation { get; set; }

        [Required]
        public int SortOrder { get; set; }

        public virtual ICollection<NodeTypeFieldOption> Options { get; set; } = new List<NodeTypeFieldOption>();
        public virtual ICollection<NodeFieldValue> NodeFieldValues { get; set; } = new List<NodeFieldValue>();
    }
}
