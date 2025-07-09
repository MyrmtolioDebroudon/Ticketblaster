using TicketBlaster.Shared.Models;

namespace TicketBlaster.Client.Services
{
    public interface IAuthenticationService
    {
        bool IsAuthenticated { get; }
        User? CurrentUser { get; }
        string? AccessToken { get; }
        event Action? AuthenticationStateChanged;

        Task<LoginResult> LoginAsync(string username, string password);
        Task<RegisterResult> RegisterAsync(string email, string password, string firstName, string lastName, string? phoneNumber = null);
        Task LogoutAsync();
        Task<bool> RefreshTokenAsync();
        Task<UserProfile?> GetUserProfileAsync();
        Task<bool> UpdateUserProfileAsync(UserProfile profile);
        Task<bool> IsInRoleAsync(string role);
        Task<List<string>> GetUserRolesAsync();
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public User? User { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }

    public class RegisterResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Message { get; set; }
    }
}