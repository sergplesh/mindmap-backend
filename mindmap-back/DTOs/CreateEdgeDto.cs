using System.ComponentModel.DataAnnotations;

namespace KnowledgeMap.Backend.DTOs
{
    public class CreateEdgeDto
    {
        [Required]
        public int MapId { get; set; }

        [Required]
        public int SourceNodeId { get; set; }

        [Required]
        public int TargetNodeId { get; set; }

        // Тип может быть либо системным, либо пользовательским
        public int? TypeId { get; set; }
        public int? CustomTypeId { get; set; }

        public bool IsHierarchy { get; set; } = true;
    }
}
