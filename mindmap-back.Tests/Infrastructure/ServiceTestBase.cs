using System.Reflection;
using System.Security.Claims;
using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace mindmap_back.Tests.Infrastructure;

public abstract class ServiceTestBase
{
    protected static ApplicationDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    protected static TokenService CreateTokenService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super_secret_test_key_1234567890",
                ["Jwt:Issuer"] = "mindmap-tests",
                ["Jwt:Audience"] = "mindmap-users",
                ["Jwt:ExpiryInMinutes"] = "60"
            })
            .Build();

        return new TokenService(configuration);
    }

    protected static T Get<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        Assert.NotNull(property);

        var value = property!.GetValue(source);
        Assert.NotNull(value);

        return Assert.IsType<T>(value);
    }

    protected static T WithUser<T>(T controller, int userId) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                    "TestAuth"))
            }
        };

        return controller;
    }

    protected static async Task<(User owner, User learner, User outsider, Map map, Node root, Node child)> SeedBasicMapAsync(
        ApplicationDbContext context,
        bool grantLearnerAccess = true,
        string learnerRole = "learner",
        bool withChildQuestion = false)
    {
        var now = DateTime.UtcNow;

        var owner = new User { Username = $"owner_{Guid.NewGuid():N}", PasswordHash = "hash" };
        var learner = new User { Username = $"learner_{Guid.NewGuid():N}", PasswordHash = "hash" };
        var outsider = new User { Username = $"outsider_{Guid.NewGuid():N}", PasswordHash = "hash" };

        context.Users.AddRange(owner, learner, outsider);
        await context.SaveChangesAsync();

        var map = new Map
        {
            OwnerId = owner.Id,
            Title = "Map",
            Description = "Map description",
            Emoji = "M",
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Maps.Add(map);
        await context.SaveChangesAsync();

        var root = new Node
        {
            MapId = map.Id,
            Title = "Root",
            Description = "Root description",
            XPosition = 0,
            YPosition = 0,
            CreatedAt = now,
            UpdatedAt = now,
            RequiresQuiz = false
        };

        var child = new Node
        {
            MapId = map.Id,
            Title = "Child",
            Description = "Child description",
            XPosition = 100,
            YPosition = 100,
            CreatedAt = now,
            UpdatedAt = now,
            RequiresQuiz = true
        };

        context.Nodes.AddRange(root, child);
        await context.SaveChangesAsync();

        context.Edges.Add(new Edge
        {
            SourceNodeId = root.Id,
            TargetNodeId = child.Id,
            IsHierarchy = true,
            CreatedAt = now
        });

        if (grantLearnerAccess)
        {
            context.Accesses.Add(new Access
            {
                MapId = map.Id,
                UserId = learner.Id,
                Role = learnerRole
            });
        }

        if (withChildQuestion)
        {
            var question = new Question
            {
                NodeId = child.Id,
                QuestionText = "Question?",
                QuestionType = "single_choice"
            };

            context.Questions.Add(question);
            await context.SaveChangesAsync();

            context.AnswerOptions.AddRange(
                new AnswerOption { QuestionId = question.Id, OptionText = "Right", IsCorrect = true },
                new AnswerOption { QuestionId = question.Id, OptionText = "Wrong", IsCorrect = false });
        }

        await context.SaveChangesAsync();
        return (owner, learner, outsider, map, root, child);
    }
}
