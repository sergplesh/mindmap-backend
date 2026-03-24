using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace KnowledgeMap.Backend.Middleware
{
    public class TokenMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Проверяем, есть ли заголовок Authorization
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            // Если заголовок есть и он не начинается с "Bearer ", но содержит только токен
            if (!string.IsNullOrEmpty(authHeader) && !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                // Добавляем "Bearer " перед токеном
                context.Request.Headers["Authorization"] = $"Bearer {authHeader}";
            }

            await _next(context);
        }
    }

    // Extension method для удобного добавления в pipeline
    public static class TokenMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenMiddleware>();
        }
    }
}