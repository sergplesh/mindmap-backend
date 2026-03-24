using System.ComponentModel.DataAnnotations;

namespace KnowledgeMap.Backend.DTOs
{
    public class UpdateEdgeDto
    {
        // Тип может быть либо системным, либо пользовательским
        public int? TypeId { get; set; }
        public int? CustomTypeId { get; set; }
    }
}