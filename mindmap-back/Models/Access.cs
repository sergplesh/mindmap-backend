using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeMap.Backend.Models
{
    public class Access
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MapId { get; set; }

        [ForeignKey("MapId")]
        public virtual Map Map { get; set; } = null!;

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;
    }
}