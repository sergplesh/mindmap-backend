using KnowledgeMap.Backend.Controllers;
using KnowledgeMap.Backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using mindmap_back.Tests.Infrastructure;

namespace mindmap_back.Tests.Controllers;

public sealed class AuthControllerTests : ServiceTestBase
{
    [Fact]
    public async Task Register_ReturnsOk()
    {
        await using var context = CreateContext();
        var controller = new AuthController(context, CreateTokenService());

        var result = await controller.Register(new RegisterDto
        {
            Username = "alice",
            Password = "password123"
        });

        Assert.IsType<OkObjectResult>(result);
    }
}
