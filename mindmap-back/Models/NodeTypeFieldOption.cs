using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class NodeTypeFieldOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NodeTypeFieldDefinitionId { get; set; }

        [ForeignKey(nameof(NodeTypeFieldDefinitionId))]
        public virtual NodeTypeFieldDefinition FieldDefinition { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Value { get; set; } = string.Empty;

        [Required]
        public int SortOrder { get; set; }
    }
}
