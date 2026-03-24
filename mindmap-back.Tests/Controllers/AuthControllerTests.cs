using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_CreatesUserWithHashedPasswordAndReturnsToken()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var controller = CreateController(context);

        var result = await controller.Register(new RegisterDto
        {
            Username = "alice",
            Password = "secret123"
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);
        var savedUser = await context.Users.SingleAsync();

        Assert.Equal("alice", savedUser.Username);
        Assert.NotEqual("secret123", savedUser.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("secret123", savedUser.PasswordHash));
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal(savedUser.Id, response.User.Id);
        Assert.Equal("alice", response.User.Username);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenUsernameAlreadyExists()
    {
        await using var context = TestDbContextFactory.CreateContext();
        context.Users.Add(new User
        {
            Id = 1,
            Username = "alice",
            PasswordHash = "hash"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.Register(new RegisterDto
        {
            Username = "alice",
            Password = "secret123"
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(1, await context.Users.CountAsync());
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsAreValid()
    {
        await using var context = TestDbContextFactory.CreateContext();
        context.Users.Add(new User
        {
            Id = 1,
            Username = "alice",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret123")
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.Login(new LoginDto
        {
            Username = "alice",
            Password = "secret123"
        });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponseDto>(okResult.Value);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal(1, response.User.Id);
        Assert.Equal("alice", response.User.Username);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        await using var context = TestDbContextFactory.CreateContext();
        context.Users.Add(new User
        {
            Id = 1,
            Username = "alice",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret123")
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.Login(new LoginDto
        {
            Username = "alice",
            Password = "wrong-password"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var controller = CreateController(context);

        var result = await controller.Login(new LoginDto
        {
            Username = "missing",
            Password = "secret123"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsCurrentUserDto()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var user = new User
        {
            Id = 5,
            Username = "current-user",
            PasswordHash = "hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var controller = CreateController(context).WithAuthenticatedUser(user.Id, user.Username);

        var result = await controller.GetCurrentUser();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<UserDto>(okResult.Value);

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Username, dto.Username);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsNotFound_WhenUserNoLongerExists()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var controller = CreateController(context).WithAuthenticatedUser(999, "ghost");

        var result = await controller.GetCurrentUser();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CheckUserExists_ReturnsNotFoundForUnknownUser()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var controller = CreateController(context);

        var result = await controller.CheckUserExists("ghost");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
        Assert.False(AnonymousObjectReader.Get<bool>(notFound.Value!, "exists"));
    }

    [Fact]
    public async Task CheckUserExists_ReturnsBadRequest_WhenUsernameIsBlank()
    {
        await using var context = TestDbContextFactory.CreateContext();
        var controller = CreateController(context);

        var result = await controller.CheckUserExists("   ");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CheckUserExists_ReturnsUserPayload_WhenUserExists()
    {
        await using var context = TestDbContextFactory.CreateContext();
        context.Users.Add(new User
        {
            Id = 1,
            Username = "alice",
            PasswordHash = "hash"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.CheckUserExists("alice");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.True(AnonymousObjectReader.Get<bool>(okResult.Value!, "exists"));
        Assert.Equal(1, AnonymousObjectReader.Get<int>(okResult.Value!, "userId"));
        Assert.Equal("alice", AnonymousObjectReader.Get<string>(okResult.Value!, "username"));
    }

    private static AuthController CreateController(KnowledgeMap.Backend.Data.ApplicationDbContext context)
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

        return new AuthController(context, new TokenService(configuration));
    }
}
