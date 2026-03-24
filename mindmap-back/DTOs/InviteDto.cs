using System.ComponentModel.DataAnnotations;

namespace KnowledgeMap.Backend.DTOs
{
    public class InviteDto
    {
        [Required]
        public int MapId { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(observer|learner)$")]
        public string Role { get; set; } = "observer";
    }
}