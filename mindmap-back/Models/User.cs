using System.ComponentModel.DataAnnotations;

namespace KnowledgeMap.Backend.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public virtual ICollection<Map> OwnedMaps { get; set; } = new List<Map>();
        public virtual ICollection<Access> Accesses { get; set; } = new List<Access>();
        public virtual ICollection<LearningProgress> LearningProgresses { get; set; } = new List<LearningProgress>();
        public virtual ICollection<AnswerResult> AnswerResults { get; set; } = new List<AnswerResult>();
    }
}