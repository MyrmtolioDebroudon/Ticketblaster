using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class KeycloakService : IKeycloakService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeycloakService> _logger;
        private readonly string _realm;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _baseUrl;

        public KeycloakService(HttpClient httpClient, IConfiguration configuration, ILogger<KeycloakService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            
            _baseUrl = _configuration["Keycloak:Authority"] ?? throw new InvalidOperationException("Keycloak:Authority not configured");
            _realm = ExtractRealmFromAuthority(_baseUrl);
            _clientId = _configuration["Keycloak:ClientId"] ?? throw new InvalidOperationException("Keycloak:ClientId not configured");
            _clientSecret = _configuration["Keycloak:ClientSecret"] ?? throw new InvalidOperationException("Keycloak:ClientSecret not configured");
        }

        private string ExtractRealmFromAuthority(string authority)
        {
            var uri = new Uri(authority);
            var segments = uri.Segments;
            return segments[segments.Length - 1].TrimEnd('/');
        }

        private async Task<string> GetAdminAccessTokenAsync()
        {
            var tokenEndpoint = $"{_baseUrl}/protocol/openid-connect/token";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.PostAsync(tokenEndpoint, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);
            return tokenResponse.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("No access token received");
        }

        public async Task<KeycloakUser?> GetUserByIdAsync(string keycloakId)
        {
            try
            {
                var token = await GetAdminAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get user {UserId}: {StatusCode}", keycloakId, response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<KeycloakUser>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", keycloakId);
                return null;
            }
        }

        public async Task<KeycloakUser?> GetUserByEmailAsync(string email)
        {
            try
            {
                var token = await GetAdminAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/admin/realms/{_realm}/users?email={email}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get user by email {Email}: {StatusCode}", email, response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<KeycloakUser>>(json);
                return users?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email {Email}", email);
                return null;
            }
        }

        public async Task<KeycloakUser> CreateUserAsync(CreateKeycloakUserRequest request)
        {
            try
            {
                var token = await GetAdminAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/admin/realms/{_realm}/users", content);
                response.EnsureSuccessStatusCode();

                // Get the created user
                var location = response.Headers.Location?.ToString();
                if (string.IsNullOrEmpty(location))
                {
                    throw new InvalidOperationException("No location header in response");
                }

                var userId = location.Split('/').Last();
                var user = await GetUserByIdAsync(userId);
                return user ?? throw new InvalidOperationException("Failed to retrieve created user");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(string keycloakId, UpdateKeycloakUserRequest request)
        {
            try
            {
                var token = await GetAdminAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", keycloakId);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(string keycloakId)
        {
            try
            {
                var token = await GetAdminAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.DeleteAsync($"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", keycloakId);
                return false;
            }
        }

        public async Task<bool> AssignRoleAsync(string keycloakId, string roleName)
        {
            try
            {
                var token = await GetAdminAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Get role info
                var roleResponse = await _httpClient.GetAsync($"{_baseUrl}/admin/realms/{_realm}/roles/{roleName}");
                if (!roleResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Role {RoleName} not found", roleName);
                    return false;
                }

                var roleJson = await roleResponse.Content.ReadAsStringAsync();
                var role = JsonSerializer.Deserialize<JsonElement>(roleJson);

                // Assign role to user
                var content = new StringContent($"[{roleJson}]", Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}/role-mappings/realm", content);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}", roleName, keycloakId);
                return false;
            }
        }

        public async Task<bool> RemoveRoleAsync(string keycloakId, string roleName)
        {
            try
            {
                var token = await GetAdminAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Get role info
                var roleResponse = await _httpClient.GetAsync($"{_baseUrl}/admin/realms/{_realm}/roles/{roleName}");
                if (!roleResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Role {RoleName} not found", roleName);
                    return false;
                }

                var roleJson = await roleResponse.Content.ReadAsStringAsync();

                // Remove role from user
                var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}/role-mappings/realm");
                request.Content = new StringContent($"[{roleJson}]", Encoding.UTF8, "application/json");
                
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", roleName, keycloakId);
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string keycloakId)
        {
            try
            {
                var token = await GetAdminAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}/role-mappings/realm");
                if (!response.IsSuccessStatusCode)
                {
                    return new List<string>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var roles = JsonSerializer.Deserialize<List<JsonElement>>(json);
                return roles?.Select(r => r.GetProperty("name").GetString() ?? string.Empty) ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user {UserId}", keycloakId);
                return new List<string>();
            }
        }

        public async Task<bool> ResetPasswordAsync(string keycloakId, string temporaryPassword)
        {
            try
            {
                var token = await GetAdminAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var passwordReset = new
                {
                    type = "password",
                    value = temporaryPassword,
                    temporary = true
                };

                var json = JsonSerializer.Serialize(passwordReset);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}/reset-password", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", keycloakId);
                return false;
            }
        }

        public async Task<bool> SendVerificationEmailAsync(string keycloakId)
        {
            try
            {
                var token = await GetAdminAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PutAsync($"{_baseUrl}/admin/realms/{_realm}/users/{keycloakId}/send-verify-email", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification email for user {UserId}", keycloakId);
                return false;
            }
        }

        public async Task<bool> VerifyEmailAsync(string keycloakId)
        {
            var updateRequest = new UpdateKeycloakUserRequest
            {
                EmailVerified = true
            };
            return await UpdateUserAsync(keycloakId, updateRequest);
        }

        public async Task<bool> EnableUserAsync(string keycloakId)
        {
            var updateRequest = new UpdateKeycloakUserRequest
            {
                Enabled = true
            };
            return await UpdateUserAsync(keycloakId, updateRequest);
        }

        public async Task<bool> DisableUserAsync(string keycloakId)
        {
            var updateRequest = new UpdateKeycloakUserRequest
            {
                Enabled = false
            };
            return await UpdateUserAsync(keycloakId, updateRequest);
        }

        public async Task<TokenResponse> GetAccessTokenAsync(string username, string password)
        {
            try
            {
                var tokenEndpoint = $"{_baseUrl}/protocol/openid-connect/token";
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password)
                });

                var response = await _httpClient.PostAsync(tokenEndpoint, content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<JsonElement>(json);

                return new TokenResponse
                {
                    AccessToken = tokenData.GetProperty("access_token").GetString() ?? string.Empty,
                    RefreshToken = tokenData.GetProperty("refresh_token").GetString() ?? string.Empty,
                    ExpiresIn = tokenData.GetProperty("expires_in").GetInt32(),
                    TokenType = tokenData.GetProperty("token_type").GetString() ?? "Bearer",
                    Scope = tokenData.TryGetProperty("scope", out var scope) ? scope.GetString() ?? string.Empty : string.Empty,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.GetProperty("expires_in").GetInt32())
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting access token for user {Username}", username);
                throw;
            }
        }

        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var tokenEndpoint = $"{_baseUrl}/protocol/openid-connect/token";
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken)
                });

                var response = await _httpClient.PostAsync(tokenEndpoint, content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<JsonElement>(json);

                return new TokenResponse
                {
                    AccessToken = tokenData.GetProperty("access_token").GetString() ?? string.Empty,
                    RefreshToken = tokenData.GetProperty("refresh_token").GetString() ?? string.Empty,
                    ExpiresIn = tokenData.GetProperty("expires_in").GetInt32(),
                    TokenType = tokenData.GetProperty("token_type").GetString() ?? "Bearer",
                    Scope = tokenData.TryGetProperty("scope", out var scope) ? scope.GetString() ?? string.Empty : string.Empty,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenData.GetProperty("expires_in").GetInt32())
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                throw;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var introspectEndpoint = $"{_baseUrl}/protocol/openid-connect/token/introspect";
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("token", token)
                });

                var response = await _httpClient.PostAsync(introspectEndpoint, content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);
                
                return result.GetProperty("active").GetBoolean();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return false;
            }
        }

        public async Task<UserInfo> GetUserInfoAsync(string token)
        {
            try
            {
                var userInfoEndpoint = $"{_baseUrl}/protocol/openid-connect/userinfo";
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync(userInfoEndpoint);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var userData = JsonSerializer.Deserialize<JsonElement>(json);

                return new UserInfo
                {
                    Sub = userData.GetProperty("sub").GetString() ?? string.Empty,
                    Name = userData.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                    PreferredUsername = userData.TryGetProperty("preferred_username", out var username) ? username.GetString() ?? string.Empty : string.Empty,
                    GivenName = userData.TryGetProperty("given_name", out var givenName) ? givenName.GetString() ?? string.Empty : string.Empty,
                    FamilyName = userData.TryGetProperty("family_name", out var familyName) ? familyName.GetString() ?? string.Empty : string.Empty,
                    Email = userData.TryGetProperty("email", out var email) ? email.GetString() ?? string.Empty : string.Empty,
                    EmailVerified = userData.TryGetProperty("email_verified", out var emailVerified) && emailVerified.GetBoolean(),
                    Roles = userData.TryGetProperty("realm_access", out var realmAccess) && realmAccess.TryGetProperty("roles", out var rolesElement)
                        ? rolesElement.EnumerateArray().Select(r => r.GetString() ?? string.Empty).ToList()
                        : new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                throw;
            }
        }
    }
}