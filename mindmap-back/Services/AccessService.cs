using KnowledgeMap.Backend.DTOs;
using KnowledgeMap.Backend.Models;
using KnowledgeMap.Backend.Repositories;

namespace KnowledgeMap.Backend.Services
{
    public interface IAccessService
    {
        Task<ServiceResult> InviteUserAsync(int currentUserId, InviteDto inviteDto);
        Task<ServiceResult> GetMapAccessAsync(int currentUserId, int mapId);
        Task<ServiceResult> UpdateRoleAsync(int currentUserId, int accessId, UpdateRoleDto updateRoleDto);
        Task<ServiceResult> RemoveAccessAsync(int currentUserId, int accessId);
    }

    public class AccessService : IAccessService
    {
        private readonly IAccessRepository _repository;

        public AccessService(IAccessRepository repository)
        {
            _repository = repository;
        }

        public async Task<ServiceResult> InviteUserAsync(int currentUserId, InviteDto inviteDto)
        {
            var map = await _repository.GetMapWithOwnerAsync(inviteDto.MapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != currentUserId)
            {
                return ServiceResult.Forbidden();
            }

            var user = await _repository.GetUserByUsernameAsync(inviteDto.Username);
            if (user == null)
            {
                return ServiceResult.NotFound(new { message = "Пользователь не найден" });
            }

            if (user.Id == currentUserId)
            {
                return ServiceResult.BadRequest(new { message = "Нельзя пригласить самого себя" });
            }

            var existingAccess = await _repository.GetAccessByMapAndUserAsync(inviteDto.MapId, user.Id);
            if (existingAccess != null)
            {
                return ServiceResult.BadRequest(new { message = "У пользователя уже есть доступ к этой карте" });
            }

            var access = new Access
            {
                MapId = inviteDto.MapId,
                UserId = user.Id,
                Role = inviteDto.Role
            };

            await _repository.AddAccessAsync(access);

            return ServiceResult.Success(new
            {
                message = "Пользователь приглашён",
                access = new
                {
                    id = access.Id,
                    accessId = access.Id,
                    userId = user.Id,
                    username = user.Username,
                    role = access.Role
                }
            });
        }

        public async Task<ServiceResult> GetMapAccessAsync(int currentUserId, int mapId)
        {
            var map = await _repository.GetMapByIdAsync(mapId);
            if (map == null)
            {
                return ServiceResult.NotFound(new { message = "Карта не найдена" });
            }

            if (map.OwnerId != currentUserId)
            {
                return ServiceResult.Forbidden();
            }

            var accesses = await _repository.GetAccessesForMapAsync(mapId);
            var result = accesses.Select(a => new
            {
                id = a.Id,
                accessId = a.Id,
                userId = a.User.Id,
                username = a.User.Username,
                role = a.Role
            }).ToList();

            return ServiceResult.Success(result);
        }

        public async Task<ServiceResult> UpdateRoleAsync(int currentUserId, int accessId, UpdateRoleDto updateRoleDto)
        {
            var access = await _repository.GetAccessWithMapAsync(accessId);
            if (access == null)
            {
                return ServiceResult.NotFound(new { message = "Доступ не найден" });
            }

            if (access.Map.OwnerId != currentUserId)
            {
                return ServiceResult.Forbidden();
            }

            access.Role = updateRoleDto.Role;
            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Роль обновлена" });
        }

        public async Task<ServiceResult> RemoveAccessAsync(int currentUserId, int accessId)
        {
            var access = await _repository.GetAccessWithMapAsync(accessId);
            if (access == null)
            {
                return ServiceResult.NotFound(new { message = "Доступ не найден" });
            }

            if (access.Map.OwnerId != currentUserId)
            {
                return ServiceResult.Forbidden();
            }

            _repository.RemoveAccess(access);
            await _repository.SaveChangesAsync();

            return ServiceResult.Success(new { message = "Доступ удалён" });
        }
    }
}
