using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Client.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<AuthenticationService> _logger;
        
        private const string TokenKey = "authToken";
        private const string RefreshTokenKey = "refreshToken";
        private const string UserKey = "currentUser";

        public bool IsAuthenticated => CurrentUser != null;
        public User? CurrentUser { get; private set; }
        public string? AccessToken { get; private set; }
        
        public event Action? AuthenticationStateChanged;

        public AuthenticationService(
            HttpClient httpClient,
            AuthenticationStateProvider authStateProvider,
            IJSRuntime jsRuntime,
            ILogger<AuthenticationService> logger)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
            _jsRuntime = jsRuntime;
            _logger = logger;
            
            // Initialize from local storage
            Task.Run(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            try
            {
                AccessToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
                var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", UserKey);
                
                if (!string.IsNullOrEmpty(userJson))
                {
                    CurrentUser = JsonSerializer.Deserialize<User>(userJson);
                }

                if (!string.IsNullOrEmpty(AccessToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing authentication service");
            }
        }

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new
                {
                    Username = username,
                    Password = password
                });

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    dynamic? result = JsonSerializer.Deserialize<dynamic>(content);
                    
                    if (result != null)
                    {
                        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content);
                        if (loginResponse?.Success == true && loginResponse.User != null)
                        {
                            // Store tokens and user info
                            AccessToken = loginResponse.AccessToken;
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, AccessToken);
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, loginResponse.RefreshToken);
                            
                            CurrentUser = new User
                            {
                                UserId = loginResponse.User.UserId,
                                Email = loginResponse.User.Email,
                                FirstName = loginResponse.User.FirstName,
                                LastName = loginResponse.User.LastName
                            };
                            
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserKey, JsonSerializer.Serialize(CurrentUser));
                            
                            // Update HTTP client authorization header
                            _httpClient.DefaultRequestHeaders.Authorization = 
                                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
                            
                            // Notify authentication state changed
                            ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(AccessToken);
                            AuthenticationStateChanged?.Invoke();
                            
                            return new LoginResult
                            {
                                Success = true,
                                User = CurrentUser,
                                AccessToken = AccessToken,
                                RefreshToken = loginResponse.RefreshToken
                            };
                        }
                    }
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                dynamic? errorResult = JsonSerializer.Deserialize<dynamic>(errorContent);
                
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = errorResult?.ErrorMessage ?? "Login failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred during login"
                };
            }
        }

        public async Task<RegisterResult> RegisterAsync(string email, string password, string firstName, string lastName, string? phoneNumber = null)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/register", new
                {
                    Email = email,
                    Password = password,
                    FirstName = firstName,
                    LastName = lastName,
                    PhoneNumber = phoneNumber
                });

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RegisterResponse>(content);
                    
                    return new RegisterResult
                    {
                        Success = result?.Success ?? false,
                        Message = result?.Message
                    };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                dynamic? errorResult = JsonSerializer.Deserialize<dynamic>(errorContent);
                
                return new RegisterResult
                {
                    Success = false,
                    ErrorMessage = errorResult?.ErrorMessage ?? "Registration failed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return new RegisterResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred during registration"
                };
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                // Call logout endpoint
                await _httpClient.PostAsync("/api/auth/logout", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling logout endpoint");
            }
            
            // Clear local state
            CurrentUser = null;
            AccessToken = null;
            
            // Clear local storage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserKey);
            
            // Clear authorization header
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            // Notify authentication state changed
            ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
            AuthenticationStateChanged?.Invoke();
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var refreshToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return false;
                }

                var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", new
                {
                    RefreshToken = refreshToken
                });

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TokenResponse>(content);
                    
                    if (result != null)
                    {
                        AccessToken = result.AccessToken;
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, AccessToken);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, result.RefreshToken);
                        
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
                        
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
            }
            
            return false;
        }

        public async Task<UserProfile?> GetUserProfileAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/user/profile");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserProfile>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
            }
            
            return null;
        }

        public async Task<bool> UpdateUserProfileAsync(UserProfile profile)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("/api/user/profile", profile);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return false;
            }
        }

        public async Task<bool> IsInRoleAsync(string role)
        {
            var roles = await GetUserRolesAsync();
            return roles.Contains(role);
        }

        public async Task<List<string>> GetUserRolesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/user/roles");
                if (response.IsSuccessStatusCode)
                {
                    var roles = await response.Content.ReadFromJsonAsync<List<Role>>();
                    return roles?.Select(r => r.Name).ToList() ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles");
            }
            
            return new List<string>();
        }

        // DTOs for API responses
        private class LoginResponse
        {
            public bool Success { get; set; }
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public int ExpiresIn { get; set; }
            public UserDto? User { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }

        private class RegisterResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int UserId { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }

        private class TokenResponse
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public int ExpiresIn { get; set; }
            public string TokenType { get; set; } = string.Empty;
        }

        private class UserDto
        {
            public int UserId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public List<string> Roles { get; set; } = new List<string>();
        }
    }
}