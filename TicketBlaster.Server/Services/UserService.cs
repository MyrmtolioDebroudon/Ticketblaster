using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class UserService : IUserService
    {
        public Task<User?> GetUserAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<User?> GetUserByEmailAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<User> CreateUserAsync(UserRegistration registration)
        {
            throw new NotImplementedException();
        }

        public Task<User> UpdateUserAsync(User user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteUserAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public Task<UserProfile> GetUserProfileAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<UserProfile> UpdateUserProfileAsync(UserProfile profile)
        {
            throw new NotImplementedException();
        }
    }
}