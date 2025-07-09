using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class KeycloakService : IKeycloakService
    {
        public Task<string> GetAccessTokenAsync()
        {
            throw new NotImplementedException();
        }

        public Task<User?> GetUserAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<User> CreateUserAsync(UserRegistration registration)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateUserAsync(User user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteUserAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AssignRoleAsync(string userId, string roleName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveRoleAsync(string userId, string roleName)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ResetPasswordAsync(string userId, string newPassword)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendVerificationEmailAsync(string userId)
        {
            throw new NotImplementedException();
        }

        public Task<UserInfo?> GetUserInfoAsync(string accessToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateTokenAsync(string accessToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> RefreshTokenAsync(string refreshToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LogoutAsync(string refreshToken)
        {
            throw new NotImplementedException();
        }
    }
}