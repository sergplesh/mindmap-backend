using KnowledgeMap.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace mindmap_back.Tests.Infrastructure;

internal static class TestDbContextFactory
{
    public static ApplicationDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
