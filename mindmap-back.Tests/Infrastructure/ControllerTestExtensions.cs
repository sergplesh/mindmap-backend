using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace mindmap_back.Tests.Infrastructure;

internal static class ControllerTestExtensions
{
    public static T WithAuthenticatedUser<T>(this T controller, int userId, string username = "test-user")
        where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                            new Claim(ClaimTypes.Name, username),
                        },
                        "TestAuth"))
            }
        };

        return controller;
    }
}
