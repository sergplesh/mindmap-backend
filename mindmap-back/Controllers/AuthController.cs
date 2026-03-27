using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Repositories;
using KnowledgeMap.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeMap.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        [ActivatorUtilitiesConstructor]
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        public AuthController(ApplicationDbContext context, TokenService tokenService)
            : this(new AuthService(new AuthRepository(context), tokenService))
        {
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            return HandleServiceResult(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            return HandleServiceResult(result);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var result = await _authService.GetCurrentUserAsync(GetCurrentUserId());
            return HandleServiceResult(result);
        }

        [HttpGet("check-user/{username}")]
        public async Task<IActionResult> CheckUserExists(string username)
        {
            var result = await _authService.CheckUserExistsAsync(username);
            return HandleServiceResult(result);
        }
    }
}
