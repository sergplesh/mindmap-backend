using BCrypt.Net;
using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;

namespace KnowledgeMap.Backend.Services
{
    public interface IAuthService
    {
        Task<ServiceResult> RegisterAsync(RegisterDto registerDto);
        Task<ServiceResult> LoginAsync(LoginDto loginDto);
        Task<ServiceResult> GetCurrentUserAsync(int userId);
        Task<ServiceResult> CheckUserExistsAsync(string username);
    }

    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repository;
        private readonly TokenService _tokenService;

        public AuthService(IAuthRepository repository, TokenService tokenService)
        {
            _repository = repository;
            _tokenService = tokenService;
        }

        public async Task<ServiceResult> RegisterAsync(RegisterDto registerDto)
        {
            var existingUser = await _repository.GetUserByUsernameAsync(registerDto.Username);
            if (existingUser != null)
            {
                return ServiceResult.BadRequest(new { message = "Пользователь с таким логином уже существует" });
            }

            var user = new User
            {
                Username = registerDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
            };

            await _repository.AddUserAsync(user);

            var token = _tokenService.GenerateToken(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username
            };

            return ServiceResult.Success(new AuthResponseDto
            {
                Token = token,
                User = userDto
            });
        }

        public async Task<ServiceResult> LoginAsync(LoginDto loginDto)
        {
            var user = await _repository.GetUserByUsernameAsync(loginDto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return ServiceResult.Unauthorized(new { message = "Неверный логин или пароль" });
            }

            var token = _tokenService.GenerateToken(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username
            };

            return ServiceResult.Success(new AuthResponseDto
            {
                Token = token,
                User = userDto
            });
        }

        public async Task<ServiceResult> GetCurrentUserAsync(int userId)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult.NotFound();
            }

            return ServiceResult.Success(new UserDto
            {
                Id = user.Id,
                Username = user.Username
            });
        }

        public async Task<ServiceResult> CheckUserExistsAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return ServiceResult.BadRequest(new { message = "Имя пользователя не может быть пустым" });
            }

            var user = await _repository.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return ServiceResult.NotFound(new
                {
                    exists = false,
                    message = $"Пользователь '{username}' не найден"
                });
            }

            return ServiceResult.Success(new
            {
                exists = true,
                userId = user.Id,
                username = user.Username
            });
        }
    }
}
