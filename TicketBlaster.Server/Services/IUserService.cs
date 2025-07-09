using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface IUserService
    {
        Task<User?> GetUserAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByKeycloakIdAsync(string keycloakId);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> AssignRoleAsync(int userId, int roleId);
        Task<bool> RemoveRoleAsync(int userId, int roleId);
        Task<IEnumerable<Role>> GetUserRolesAsync(int userId);
        Task<bool> IsInRoleAsync(int userId, string roleName);
        Task<UserProfile> GetUserProfileAsync(int userId);
        Task<bool> UpdateUserProfileAsync(int userId, UserProfile profile);
        Task<bool> VerifyUserAsync(int userId);
        Task<IEnumerable<User>> GetOrganizersAsync(int pageNumber = 1, int pageSize = 20);
        Task<OrganizerStats> GetOrganizerStatsAsync(int userId);
    }

    public class UserProfile
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string TimeZone { get; set; } = "UTC";
        public string Language { get; set; } = "en";
        public string Currency { get; set; } = "USD";
    }

    public class OrganizerStats
    {
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int CompletedEvents { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalAttendees { get; set; }
        public decimal AverageRating { get; set; }
        public DateTime? LastEventDate { get; set; }
        public DateTime? NextEventDate { get; set; }
    }
}