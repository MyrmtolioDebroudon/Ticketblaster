using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface IKeycloakService
    {
        Task<KeycloakUser?> GetUserByIdAsync(string keycloakId);
        Task<KeycloakUser?> GetUserByEmailAsync(string email);
        Task<KeycloakUser> CreateUserAsync(CreateKeycloakUserRequest request);
        Task<bool> UpdateUserAsync(string keycloakId, UpdateKeycloakUserRequest request);
        Task<bool> DeleteUserAsync(string keycloakId);
        Task<bool> AssignRoleAsync(string keycloakId, string roleName);
        Task<bool> RemoveRoleAsync(string keycloakId, string roleName);
        Task<IEnumerable<string>> GetUserRolesAsync(string keycloakId);
        Task<bool> ResetPasswordAsync(string keycloakId, string temporaryPassword);
        Task<bool> SendVerificationEmailAsync(string keycloakId);
        Task<bool> VerifyEmailAsync(string keycloakId);
        Task<bool> EnableUserAsync(string keycloakId);
        Task<bool> DisableUserAsync(string keycloakId);
        Task<TokenResponse> GetAccessTokenAsync(string username, string password);
        Task<TokenResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
        Task<UserInfo> GetUserInfoAsync(string token);
    }

    public class KeycloakUser
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedTimestamp { get; set; }
        public Dictionary<string, List<string>> Attributes { get; set; } = new();
        public List<string> Groups { get; set; } = new();
        public List<string> Roles { get; set; } = new();
    }

    public class CreateKeycloakUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Password { get; set; }
        public bool Enabled { get; set; } = true;
        public bool EmailVerified { get; set; } = false;
        public Dictionary<string, List<string>> Attributes { get; set; } = new();
        public List<string> Groups { get; set; } = new();
        public List<string> Roles { get; set; } = new();
    }

    public class UpdateKeycloakUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? Enabled { get; set; }
        public bool? EmailVerified { get; set; }
        public Dictionary<string, List<string>>? Attributes { get; set; }
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public string Scope { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class UserInfo
    {
        public string Sub { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PreferredUsername { get; set; } = string.Empty;
        public string GivenName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Groups { get; set; } = new();
    }
}