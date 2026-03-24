using System.ComponentModel.DataAnnotations;

namespace KnowledgeMap.Backend.DTOs
{
    public class UpdateRoleDto
    {
        [Required]
        [RegularExpression("^(observer|learner)$")]
        public string Role { get; set; } = string.Empty;
    }
}