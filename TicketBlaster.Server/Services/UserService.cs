using TicketBlaster.Database;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class UserService : IUserService
    {
        private readonly TicketBlasterDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(TicketBlasterDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<User?> GetUserAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<User?> GetUserByEmailAsync(string email)
        {
            throw new NotImplementedException();
        }

        public Task<User?> GetUserByKeycloakIdAsync(string keycloakId)
        {
            throw new NotImplementedException();
        }

        public Task<User> CreateUserAsync(User user)
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

        public Task<bool> AssignRoleAsync(int userId, int roleId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveRoleAsync(int userId, int roleId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Role>> GetUserRolesAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            throw new NotImplementedException();
        }

        public Task<UserProfile> GetUserProfileAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateUserProfileAsync(int userId, UserProfile profile)
        {
            throw new NotImplementedException();
        }

        public Task<bool> VerifyUserAsync(int userId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<User>> GetOrganizersAsync(int pageNumber = 1, int pageSize = 20)
        {
            throw new NotImplementedException();
        }

        public Task<OrganizerStats> GetOrganizerStatsAsync(int userId)
        {
            throw new NotImplementedException();
        }
    }
}