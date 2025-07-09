using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TicketBlaster.Server.Security;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class KeycloakService : IKeycloakService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeycloakService> _logger;
        private readonly string _authority;
        private readonly string _realm;
        private readonly string _clientId;
        private readonly string _clientSecret;

        public KeycloakService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<KeycloakService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("SecureClient");
            _configuration = configuration;
            _logger = logger;

            // Use secure configuration
            _authority = SecureConfiguration.GetSecureValue(configuration, "Keycloak:Authority");
            _realm = ExtractRealmFromAuthority(_authority);
            _clientId = SecureConfiguration.GetSecureValue(configuration, "Keycloak:ClientId");
            _clientSecret = SecureConfiguration.GetSecureValue(configuration, "Keycloak:ClientSecret");
        }

        public async Task<KeycloakUser?> GetUserByIdAsync(string keycloakId)
        {
            try
            {
                var token = await GetAdminTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"{_authority}/admin/realms/{_realm}/users/{keycloakId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get user {UserId}: {StatusCode}", keycloakId, response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<KeycloakUser>(json, GetJsonOptions());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId} from Keycloak", keycloakId);
                return null;
            }
        }

        public async Task<KeycloakUser?> GetUserByEmailAsync(string email)
        {
            try
            {
                var token = await GetAdminTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"{_authority}/admin/realms/{_realm}/users?email={Uri.EscapeDataString(email)}&exact=true");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get user by email: {StatusCode}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<KeycloakUser>>(json, GetJsonOptions());
                return users?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email from Keycloak");
                return null;
            }
        }

        public async Task<KeycloakUser> CreateUserAsync(CreateKeycloakUserRequest request)
        {
            try
            {
                var token = await GetAdminTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var user = new
                {
                    username = request.Username,
                    email = request.Email,
                    firstName = request.FirstName,
                    lastName = request.LastName,
                    enabled = request.Enabled,
                    emailVerified = request.EmailVerified,
                    attributes = request.Attributes,
                    groups = request.Groups,
                    realmRoles = request.Roles,
                    credentials = string.IsNullOrEmpty(request.Password) ? null : new[]
                    {
                        new
                        {
                            type = "password",
                            value = request.Password,
                            temporary = false
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_authority}/admin/realms/{_realm}/users", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Failed to create user: {response.StatusCode} - {error}");
                }

                // Get the created user ID from the Location header
                var location = response.Headers.Location?.ToString();
                var userId = location?.Split('/').LastOrDefault();

                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User created but ID not returned");
                }

                // Fetch and return the created user
                var createdUser = await GetUserByIdAsync(userId);
                return createdUser ?? throw new InvalidOperationException("User created but could not be retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user in Keycloak");
                throw;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("token", token),
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret)
                });

                var response = await _httpClient.PostAsync($"{_authority}/protocol/openid-connect/token/introspect", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Token validation failed: {StatusCode}", response.StatusCode);
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);
                
                return result.TryGetProperty("active", out var active) && active.GetBoolean();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return false;
            }
        }

        public async Task<TokenResponse> GetAccessTokenAsync(string username, string password)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password),
                    new KeyValuePair<string, string>("scope", "openid profile email")
                });

                var response = await _httpClient.PostAsync($"{_authority}/protocol/openid-connect/token", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Failed to get access token: {response.StatusCode} - {error}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<JsonElement>(json);

                return new TokenResponse
                {
                    AccessToken = tokenData.GetProperty("access_token").GetString() ?? string.Empty,
                    RefreshToken = tokenData.GetProperty("refresh_token").GetString() ?? string.Empty,
                    TokenType = tokenData.GetProperty("token_type").GetString() ?? "Bearer",
                    ExpiresIn = tokenData.GetProperty("expires_in").GetInt32(),
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.GetProperty("expires_in").GetInt32())
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting access token");
                throw;
            }
        }

        // Implement remaining interface methods...
        public Task<bool> UpdateUserAsync(string keycloakId, UpdateKeycloakUserRequest request)
            => throw new NotImplementedException("To be implemented");

        public Task<bool> DeleteUserAsync(string keycloakId)
            => throw new NotImplementedException("To be implemented");

        public Task<bool> AssignRoleAsync(string keycloakId, string roleName)
            => throw new NotImplementedException("To be implemented");

        public Task<bool> RemoveRoleAsync(string keycloakId, string roleName)
            => throw new NotImplementedException("To be implemented");

        public Task<IEnumerable<string>> GetUserRolesAsync(string keycloakId)
            => throw new NotImplementedException("To be implemented");

        public Task<bool> ResetPasswordAsync(string keycloakId, string temporaryPassword)
            => throw new NotImplementedException("To be implemented");

        public Task<bool> SendVerificationEmailAsync(string keycloakId)
            => throw new NotImplementedException("To be implemented");

        public Task<bool> VerifyEmailAsync(string keycloakId)
            => throw new NotImplementedException("To be implemented");

        public Task<bool> EnableUserAsync(string keycloakId)
            => throw new NotImplementedException("To be implemented");

        public Task<bool> DisableUserAsync(string keycloakId)
            => throw new NotImplementedException("To be implemented");

        public Task<TokenResponse> RefreshTokenAsync(string refreshToken)
            => throw new NotImplementedException("To be implemented");

        public Task<UserInfo> GetUserInfoAsync(string token)
            => throw new NotImplementedException("To be implemented");

        private async Task<string> GetAdminTokenAsync()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            });

            var response = await _httpClient.PostAsync($"{_authority}/protocol/openid-connect/token", content);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to get admin token: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(json);
            
            return tokenData.GetProperty("access_token").GetString() ?? 
                throw new InvalidOperationException("Access token not found in response");
        }

        private string ExtractRealmFromAuthority(string authority)
        {
            // Extract realm from authority URL like: https://keycloak/auth/realms/ticketblaster
            var parts = authority.Split('/');
            return parts.Length >= 2 ? parts[^1] : "master";
        }

        private JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }
    }
}