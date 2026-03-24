using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Services;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Services;

public class MapLearningAccessResolverTests
{
    [Fact]
    public async Task BuildAsync_ReturnsOwnerRoleAndUnlocksAllNodes()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var child = CreateNode(101, map.Id, "Child");

        context.Users.Add(owner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, child);
        context.Questions.Add(new Question
        {
            Id = 1000,
            NodeId = child.Id,
            QuestionText = "Question",
            QuestionType = "single_choice"
        });
        context.Edges.Add(new Edge
        {
            Id = 500,
            SourceNodeId = root.Id,
            TargetNodeId = child.Id,
            IsHierarchy = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var snapshot = await MapLearningAccessResolver.BuildAsync(context, map.Id, owner.Id);

        Assert.Equal("owner", snapshot.UserRole);
        Assert.True(snapshot.NodeStates[root.Id].IsVisible);
        Assert.True(snapshot.NodeStates[root.Id].IsUnlocked);
        Assert.True(snapshot.NodeStates[child.Id].IsVisible);
        Assert.True(snapshot.NodeStates[child.Id].IsUnlocked);
        Assert.True(snapshot.NodeStates[child.Id].CanReadContent);
        Assert.Empty(snapshot.PassedNodeIds);
    }

    [Fact]
    public async Task BuildAsync_ReturnsEmptySnapshot_WhenMapDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();

        var snapshot = await MapLearningAccessResolver.BuildAsync(context, 999, 1);

        Assert.Equal("observer", snapshot.UserRole);
        Assert.Empty(snapshot.NodeStates);
        Assert.Empty(snapshot.PassedNodeIds);
    }

    [Fact]
    public async Task BuildAsync_LocksQuestionNodeAndDescendantsForLearnerWithoutPassedQuiz()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var child = CreateNode(101, map.Id, "Child");
        var grandChild = CreateNode(102, map.Id, "GrandChild");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, child, grandChild);
        context.Accesses.Add(new Access
        {
            Id = 700,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        context.Questions.Add(new Question
        {
            Id = 1000,
            NodeId = child.Id,
            QuestionText = "Question",
            QuestionType = "single_choice"
        });
        context.Edges.AddRange(
            new Edge
            {
                Id = 500,
                SourceNodeId = root.Id,
                TargetNodeId = child.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            },
            new Edge
            {
                Id = 501,
                SourceNodeId = child.Id,
                TargetNodeId = grandChild.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var snapshot = await MapLearningAccessResolver.BuildAsync(context, map.Id, learner.Id);

        Assert.Equal("learner", snapshot.UserRole);
        Assert.True(snapshot.NodeStates[root.Id].IsUnlocked);
        Assert.True(snapshot.NodeStates[child.Id].IsVisible);
        Assert.False(snapshot.NodeStates[child.Id].IsUnlocked);
        Assert.False(snapshot.NodeStates[child.Id].CanReadContent);
        Assert.False(snapshot.NodeStates[grandChild.Id].IsVisible);
        Assert.False(snapshot.NodeStates[grandChild.Id].IsUnlocked);
        Assert.Empty(snapshot.PassedNodeIds);
    }

    [Fact]
    public async Task BuildAsync_UnlocksQuestionNodeAfterSuccessfulQuiz()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");
        var child = CreateNode(101, map.Id, "Child");
        var grandChild = CreateNode(102, map.Id, "GrandChild");

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.AddRange(root, child, grandChild);
        context.Accesses.Add(new Access
        {
            Id = 700,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });
        context.Questions.Add(new Question
        {
            Id = 1000,
            NodeId = child.Id,
            QuestionText = "Question",
            QuestionType = "single_choice"
        });
        context.Edges.AddRange(
            new Edge
            {
                Id = 500,
                SourceNodeId = root.Id,
                TargetNodeId = child.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            },
            new Edge
            {
                Id = 501,
                SourceNodeId = child.Id,
                TargetNodeId = grandChild.Id,
                IsHierarchy = true,
                CreatedAt = DateTime.UtcNow
            });
        context.AnswerResults.Add(new AnswerResult
        {
            Id = 900,
            UserId = learner.Id,
            NodeId = child.Id,
            IsPassed = true,
            CompletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var snapshot = await MapLearningAccessResolver.BuildAsync(context, map.Id, learner.Id);

        Assert.Contains(child.Id, snapshot.PassedNodeIds);
        Assert.True(snapshot.NodeStates[child.Id].IsUnlocked);
        Assert.True(snapshot.NodeStates[child.Id].CanReadContent);
        Assert.True(snapshot.NodeStates[grandChild.Id].IsVisible);
        Assert.True(snapshot.NodeStates[grandChild.Id].IsUnlocked);
    }

    [Fact]
    public async Task BuildAsync_UsesLoadedMapAccessCollectionToResolveRole()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var owner = CreateUser(1, "owner");
        var learner = CreateUser(2, "learner");
        var map = CreateMap(10, owner.Id);
        var root = CreateNode(100, map.Id, "Root");

        map.Accesses.Add(new Access
        {
            Id = 700,
            MapId = map.Id,
            UserId = learner.Id,
            Role = "learner"
        });

        context.Users.AddRange(owner, learner);
        context.Maps.Add(map);
        context.Nodes.Add(root);
        await context.SaveChangesAsync();

        var loadedMap = await context.Maps
            .Include(m => m.Nodes)
            .Include(m => m.Accesses)
            .FirstAsync(m => m.Id == map.Id);

        var snapshot = await MapLearningAccessResolver.BuildAsync(context, map.Id, learner.Id, loadedMap);

        Assert.Equal("learner", snapshot.UserRole);
        Assert.True(snapshot.NodeStates[root.Id].IsUnlocked);
    }

    private static User CreateUser(int id, string username) => new()
    {
        Id = id,
        Username = username,
        PasswordHash = "hash"
    };

    private static Map CreateMap(int id, int ownerId) => new()
    {
        Id = id,
        OwnerId = ownerId,
        Title = "Map",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static Node CreateNode(int id, int mapId, string title) => new()
    {
        Id = id,
        MapId = mapId,
        Title = title,
        XPosition = 0,
        YPosition = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
