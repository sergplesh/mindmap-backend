using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KnowledgeMap.Backend.Data;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Services;
using BCrypt.Net;

namespace KnowledgeMap.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(ApplicationDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            // Проверяем, не существует ли уже пользователь с таким логином
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == registerDto.Username);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Пользователь с таким логином уже существует" });
            }

            // Создаём нового пользователя
            var user = new User
            {
                Username = registerDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Генерируем токен
            var token = _tokenService.GenerateToken(user);

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username
            };

            return Ok(new AuthResponseDto
            {
                Token = token,
                User = userDto
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            // Ищем пользователя по логину
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (user == null)
            {
                return Unauthorized(new { message = "Неверный логин или пароль" });
            }

            // Проверяем пароль
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Неверный логин или пароль" });
            }

            // Генерируем токен
            var token = _tokenService.GenerateToken(user);

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username
            };

            return Ok(new AuthResponseDto
            {
                Token = token,
                User = userDto
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username
            };

            return Ok(userDto);
        }

        // GET: api/auth/check-user/{username} - проверить существование пользователя
        [HttpGet("check-user/{username}")]
        public async Task<IActionResult> CheckUserExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new { message = "Имя пользователя не может быть пустым" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound(new
                {
                    exists = false,
                    message = $"Пользователь '{username}' не найден"
                });
            }

            return Ok(new
            {
                exists = true,
                userId = user.Id,
                username = user.Username
            });
        }
    }
}