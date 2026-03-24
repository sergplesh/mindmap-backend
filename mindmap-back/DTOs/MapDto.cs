using System;

namespace KnowledgeMap.Backend.DTOs
{
    public class MapDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Emoji { get; set; }
        public int OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string UserRole { get; set; } = "observer";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int NodesCount { get; set; }
        public int EdgesCount { get; set; }
    }
}
