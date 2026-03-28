using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Repositories;
using KnowledgeMap.Backend.Services;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Services;

public sealed class AuthServiceTests : ServiceTestBase
{
    [Fact]
    public async Task CheckUserExists_EmptyUsername_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        var service = new AuthService(new AuthRepository(context), CreateTokenService());

        var result = await service.CheckUserExistsAsync(" ");

        Assert.Equal(ServiceResultType.BadRequest, result.Type);
    }

    [Fact]
    public async Task Register_NewUser_ReturnsSuccessWithToken()
    {
        await using var context = CreateContext();
        var service = new AuthService(new AuthRepository(context), CreateTokenService());

        var result = await service.RegisterAsync(new RegisterDto
        {
            Username = "alice",
            Password = "password123"
        });

        Assert.Equal(ServiceResultType.Success, result.Type);
        Assert.False(string.IsNullOrWhiteSpace(Get<string>(result.Value!, "Token")));
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsBadRequest()
    {
        await using var context = CreateContext();
        var service = new AuthService(new AuthRepository(context), CreateTokenService());

        await service.RegisterAsync(new RegisterDto { Username = "alice", Password = "password123" });
        var duplicate = await service.RegisterAsync(new RegisterDto { Username = "alice", Password = "password123" });

        Assert.Equal(ServiceResultType.BadRequest, duplicate.Type);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await using var context = CreateContext();
        var service = new AuthService(new AuthRepository(context), CreateTokenService());
        await service.RegisterAsync(new RegisterDto { Username = "alice", Password = "password123" });

        var result = await service.LoginAsync(new LoginDto
        {
            Username = "alice",
            Password = "wrong"
        });

        Assert.Equal(ServiceResultType.Unauthorized, result.Type);
    }

    [Fact]
    public async Task Login_CorrectPassword_ReturnsSuccess()
    {
        await using var context = CreateContext();
        var service = new AuthService(new AuthRepository(context), CreateTokenService());
        await service.RegisterAsync(new RegisterDto { Username = "alice", Password = "password123" });

        var result = await service.LoginAsync(new LoginDto
        {
            Username = "alice",
            Password = "password123"
        });

        Assert.Equal(ServiceResultType.Success, result.Type);
    }

    [Fact]
    public async Task GetCurrentUser_UnknownId_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var service = new AuthService(new AuthRepository(context), CreateTokenService());

        var result = await service.GetCurrentUserAsync(999999);

        Assert.Equal(ServiceResultType.NotFound, result.Type);
    }

    [Fact]
    public async Task GetCurrentUser_KnownId_ReturnsSuccess()
    {
        await using var context = CreateContext();
        var service = new AuthService(new AuthRepository(context), CreateTokenService());
        await service.RegisterAsync(new RegisterDto { Username = "alice", Password = "password123" });
        var aliceId = context.Users.Single(u => u.Username == "alice").Id;

        var result = await service.GetCurrentUserAsync(aliceId);

        Assert.Equal(ServiceResultType.Success, result.Type);
    }

    [Fact]
    public async Task CheckUserExists_UnknownUsername_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var service = new AuthService(new AuthRepository(context), CreateTokenService());

        var result = await service.CheckUserExistsAsync("unknown");

        Assert.Equal(ServiceResultType.NotFound, result.Type);
    }

    [Fact]
    public async Task CheckUserExists_KnownUsername_ReturnsSuccess()
    {
        await using var context = CreateContext();
        var service = new AuthService(new AuthRepository(context), CreateTokenService());
        await service.RegisterAsync(new RegisterDto { Username = "alice", Password = "password123" });

        var result = await service.CheckUserExistsAsync("alice");

        Assert.Equal(ServiceResultType.Success, result.Type);
    }
}
