using KnowledgeMap.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeMap.Backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Map> Maps { get; set; } = null!;
        public DbSet<NodeType> NodeTypes { get; set; } = null!;
        public DbSet<NodeTypeFieldDefinition> NodeTypeFieldDefinitions { get; set; } = null!;
        public DbSet<NodeTypeFieldOption> NodeTypeFieldOptions { get; set; } = null!;
        public DbSet<NodeFieldValue> NodeFieldValues { get; set; } = null!;
        public DbSet<Node> Nodes { get; set; } = null!;
        public DbSet<EdgeType> EdgeTypes { get; set; } = null!;
        public DbSet<Edge> Edges { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<AnswerOption> AnswerOptions { get; set; } = null!;
        public DbSet<Access> Accesses { get; set; } = null!;
        public DbSet<LearningProgress> LearningProgresses { get; set; } = null!;
        public DbSet<AnswerResult> AnswerResults { get; set; } = null!;
        public DbSet<AnswerResultSelection> AnswerResultSelections { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureIndexes(modelBuilder);
            ConfigureConstraints(modelBuilder);
            ConfigureRelationships(modelBuilder);
            SeedData(modelBuilder);
        }

        private static void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<LearningProgress>()
                .HasIndex(lp => new { lp.UserId, lp.NodeId })
                .IsUnique();

            modelBuilder.Entity<Access>()
                .HasIndex(a => new { a.MapId, a.UserId })
                .IsUnique();

            modelBuilder.Entity<NodeType>()
                .HasIndex(t => t.Name)
                .HasFilter("[MapId] IS NULL")
                .IsUnique();

            modelBuilder.Entity<NodeType>()
                .HasIndex(t => new { t.MapId, t.Name })
                .HasFilter("[MapId] IS NOT NULL")
                .IsUnique();

            modelBuilder.Entity<EdgeType>()
                .HasIndex(t => t.Name)
                .HasFilter("[MapId] IS NULL")
                .IsUnique();

            modelBuilder.Entity<EdgeType>()
                .HasIndex(t => new { t.MapId, t.Name })
                .HasFilter("[MapId] IS NOT NULL")
                .IsUnique();

            modelBuilder.Entity<NodeTypeFieldDefinition>()
                .HasIndex(f => new { f.NodeTypeId, f.Name })
                .IsUnique();

            modelBuilder.Entity<NodeTypeFieldOption>()
                .HasIndex(o => new { o.NodeTypeFieldDefinitionId, o.SortOrder })
                .IsUnique();

            modelBuilder.Entity<NodeFieldValue>()
                .HasIndex(v => new { v.NodeId, v.NodeTypeFieldDefinitionId })
                .IsUnique();

            modelBuilder.Entity<Edge>()
                .HasIndex(e => new { e.SourceNodeId, e.TargetNodeId })
                .IsUnique();

            modelBuilder.Entity<AnswerResult>()
                .HasIndex(ar => new { ar.UserId, ar.NodeId, ar.CompletedAt });

            modelBuilder.Entity<AnswerResultSelection>()
                .HasIndex(s => new { s.AnswerResultId, s.AnswerOptionId })
                .IsUnique();
        }

        private static void ConfigureConstraints(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Access>()
                .ToTable(t => t.HasCheckConstraint("CK_Access_Role", "[Role] IN ('observer', 'learner')"));

            modelBuilder.Entity<Question>()
                .ToTable(t => t.HasCheckConstraint("CK_Question_QuestionType", "[QuestionType] IN ('single_choice', 'multiple_choice')"));

            modelBuilder.Entity<NodeType>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_NodeType_Scope",
                    "(([MapId] IS NULL AND [IsSystem] = 1) OR ([MapId] IS NOT NULL AND [IsSystem] = 0))"));

            modelBuilder.Entity<EdgeType>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_EdgeType_Scope",
                    "(([MapId] IS NULL AND [IsSystem] = 1) OR ([MapId] IS NOT NULL AND [IsSystem] = 0))"));

            modelBuilder.Entity<Edge>()
                .ToTable(t => t.HasCheckConstraint("CK_Edge_NoSelfReference", "[SourceNodeId] <> [TargetNodeId]"));
        }

        private static void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Map>()
                .HasMany(m => m.Nodes)
                .WithOne(n => n.Map)
                .HasForeignKey(n => n.MapId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Map>()
                .HasMany(m => m.NodeTypes)
                .WithOne(t => t.Map)
                .HasForeignKey(t => t.MapId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Map>()
                .HasMany(m => m.EdgeTypes)
                .WithOne(t => t.Map)
                .HasForeignKey(t => t.MapId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Access>()
                .HasOne(a => a.Map)
                .WithMany(m => m.Accesses)
                .HasForeignKey(a => a.MapId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Access>()
                .HasOne(a => a.User)
                .WithMany(u => u.Accesses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LearningProgress>()
                .HasOne(lp => lp.User)
                .WithMany(u => u.LearningProgresses)
                .HasForeignKey(lp => lp.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LearningProgress>()
                .HasOne(lp => lp.Node)
                .WithMany(n => n.LearningProgresses)
                .HasForeignKey(lp => lp.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Node>()
                .HasOne(n => n.Type)
                .WithMany(t => t.Nodes)
                .HasForeignKey(n => n.TypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Node>()
                .HasMany(n => n.FieldValues)
                .WithOne(v => v.Node)
                .HasForeignKey(v => v.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NodeType>()
                .HasMany(t => t.FieldDefinitions)
                .WithOne(f => f.NodeType)
                .HasForeignKey(f => f.NodeTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NodeTypeFieldDefinition>()
                .HasMany(f => f.Options)
                .WithOne(o => o.FieldDefinition)
                .HasForeignKey(o => o.NodeTypeFieldDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NodeFieldValue>()
                .HasOne(v => v.FieldDefinition)
                .WithMany(f => f.NodeFieldValues)
                .HasForeignKey(v => v.NodeTypeFieldDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Node>()
                .HasMany(n => n.Questions)
                .WithOne(q => q.Node)
                .HasForeignKey(q => q.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Question>()
                .HasMany(q => q.AnswerOptions)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnswerResult>()
                .HasOne(ar => ar.User)
                .WithMany(u => u.AnswerResults)
                .HasForeignKey(ar => ar.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AnswerResult>()
                .HasOne(ar => ar.Node)
                .WithMany(n => n.AnswerResults)
                .HasForeignKey(ar => ar.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnswerResult>()
                .HasMany(ar => ar.Selections)
                .WithOne(s => s.AnswerResult)
                .HasForeignKey(s => s.AnswerResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnswerResultSelection>()
                .HasOne(s => s.AnswerOption)
                .WithMany()
                .HasForeignKey(s => s.AnswerOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Edge>()
                .HasOne(e => e.SourceNode)
                .WithMany(n => n.SourceEdges)
                .HasForeignKey(e => e.SourceNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Edge>()
                .HasOne(e => e.TargetNode)
                .WithMany(n => n.TargetEdges)
                .HasForeignKey(e => e.TargetNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Edge>()
                .HasOne(e => e.Type)
                .WithMany(t => t.Edges)
                .HasForeignKey(e => e.TypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NodeType>().HasData(
                new NodeType { Id = 1, MapId = null, Name = "Понятие", Color = "#3b82f6", Icon = "psychology", Shape = "rect", Size = "medium", IsSystem = true },
                new NodeType { Id = 2, MapId = null, Name = "Определение", Color = "#10b981", Icon = "description", Shape = "rect", Size = "medium", IsSystem = true },
                new NodeType { Id = 3, MapId = null, Name = "Алгоритм", Color = "#ef4444", Icon = "route", Shape = "rect", Size = "medium", IsSystem = true },
                new NodeType { Id = 4, MapId = null, Name = "Свойство", Color = "#f59e0b", Icon = "star", Shape = "rect", Size = "medium", IsSystem = true },
                new NodeType { Id = 5, MapId = null, Name = "Теорема", Color = "#8b5cf6", Icon = "calculate", Shape = "rect", Size = "medium", IsSystem = true },
                new NodeType { Id = 6, MapId = null, Name = "Пример", Color = "#6b7280", Icon = "code", Shape = "rect", Size = "medium", IsSystem = true }
            );

            modelBuilder.Entity<EdgeType>().HasData(
                new EdgeType { Id = 1, MapId = null, Name = "is_a", Style = "solid", Label = "является", Color = "#666666", IsSystem = true },
                new EdgeType { Id = 2, MapId = null, Name = "uses", Style = "dashed", Label = "использует", Color = "#666666", IsSystem = true },
                new EdgeType { Id = 3, MapId = null, Name = "requires", Style = "solid", Label = "требует", Color = "#666666", IsSystem = true },
                new EdgeType { Id = 4, MapId = null, Name = "contrasts", Style = "dotted", Label = "отличие", Color = "#666666", IsSystem = true },
                new EdgeType { Id = 5, MapId = null, Name = "proves", Style = "dashed", Label = "доказывает", Color = "#666666", IsSystem = true }
            );
        }
    }
}
