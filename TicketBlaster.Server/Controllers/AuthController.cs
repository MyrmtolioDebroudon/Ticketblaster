using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TicketBlaster.Server.Services;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IKeycloakService _keycloakService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IKeycloakService keycloakService,
            IUserService userService,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _keycloakService = keycloakService;
            _userService = userService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Authenticate with Keycloak
                var tokenResponse = await _keycloakService.GetAccessTokenAsync(request.Username, request.Password);

                // Get user info from Keycloak
                var userInfo = await _keycloakService.GetUserInfoAsync(tokenResponse.AccessToken);

                // Get or create user in local database
                var user = await _userService.GetUserByKeycloakIdAsync(userInfo.Sub);
                if (user == null)
                {
                    // First time login - create user in local database
                    user = new User
                    {
                        KeycloakId = userInfo.Sub,
                        Email = userInfo.Email,
                        FirstName = userInfo.GivenName,
                        LastName = userInfo.FamilyName,
                        IsVerified = userInfo.EmailVerified,
                        IsActive = true,
                        LastLoginOn = DateTime.UtcNow
                    };

                    user = await _userService.CreateUserAsync(user);
                }
                else
                {
                    // Update last login
                    user.LastLoginOn = DateTime.UtcNow;
                    await _userService.UpdateUserAsync(user);
                }

                // Get user roles
                var roles = await _userService.GetUserRolesAsync(user.UserId);

                return Ok(new LoginResponse
                {
                    Success = true,
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    ExpiresIn = tokenResponse.ExpiresIn,
                    User = new UserDto
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Roles = roles.Select(r => r.Name).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user {Username}", request.Username);
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid username or password"
                });
            }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userService.GetUserByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new RegisterResponse
                    {
                        Success = false,
                        ErrorMessage = "User with this email already exists"
                    });
                }

                // Create user in Keycloak
                var keycloakRequest = new CreateKeycloakUserRequest
                {
                    Username = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Password = request.Password,
                    Enabled = true,
                    EmailVerified = false
                };

                var keycloakUser = await _keycloakService.CreateUserAsync(keycloakRequest);

                // Create user in local database
                var user = new User
                {
                    KeycloakId = keycloakUser.Id,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    IsActive = true,
                    IsVerified = false
                };

                user = await _userService.CreateUserAsync(user);

                // Send verification email
                await _keycloakService.SendVerificationEmailAsync(keycloakUser.Id);

                // Send welcome email
                await _emailService.SendWelcomeEmailAsync(user);

                return Ok(new RegisterResponse
                {
                    Success = true,
                    Message = "Registration successful. Please check your email to verify your account.",
                    UserId = user.UserId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for email {Email}", request.Email);
                return BadRequest(new RegisterResponse
                {
                    Success = false,
                    ErrorMessage = "Registration failed. Please try again."
                });
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var tokenResponse = await _keycloakService.RefreshTokenAsync(request.RefreshToken);

                return Ok(new TokenResponse
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    ExpiresIn = tokenResponse.ExpiresIn,
                    TokenType = tokenResponse.TokenType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return Unauthorized(new { message = "Invalid refresh token" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Get current user
                var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(keycloakId))
                {
                    var user = await _userService.GetUserByKeycloakIdAsync(keycloakId);
                    if (user != null)
                    {
                        _logger.LogInformation("User {UserId} logged out", user.UserId);
                    }
                }

                // Note: Actual token revocation would be handled by Keycloak
                // This endpoint is mainly for logging and any cleanup

                return Ok(new { message = "Logout successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return Ok(new { message = "Logout completed" }); // Still return OK even if logging fails
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var user = await _userService.GetUserByEmailAsync(request.Email);
                if (user == null || string.IsNullOrEmpty(user.KeycloakId))
                {
                    // Don't reveal if user exists
                    return Ok(new { message = "If an account exists with this email, you will receive password reset instructions." });
                }

                // Generate temporary password
                var tempPassword = GenerateTemporaryPassword();
                
                // Reset password in Keycloak
                await _keycloakService.ResetPasswordAsync(user.KeycloakId, tempPassword);

                // Send password reset email
                await _emailService.SendPasswordResetAsync(user.Email, tempPassword);

                return Ok(new { message = "If an account exists with this email, you will receive password reset instructions." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Forgot password failed for email {Email}", request.Email);
                return Ok(new { message = "If an account exists with this email, you will receive password reset instructions." });
            }
        }

        [HttpPost("verify-email")]
        [Authorize]
        public async Task<IActionResult> VerifyEmail()
        {
            try
            {
                var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(keycloakId))
                {
                    return Unauthorized();
                }

                var user = await _userService.GetUserByKeycloakIdAsync(keycloakId);
                if (user == null)
                {
                    return NotFound();
                }

                await _userService.VerifyUserAsync(user.UserId);

                return Ok(new { message = "Email verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email verification failed");
                return BadRequest(new { message = "Email verification failed" });
            }
        }

        [HttpPost("resend-verification")]
        [Authorize]
        public async Task<IActionResult> ResendVerificationEmail()
        {
            try
            {
                var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(keycloakId))
                {
                    return Unauthorized();
                }

                await _keycloakService.SendVerificationEmailAsync(keycloakId);

                return Ok(new { message = "Verification email sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend verification email");
                return BadRequest(new { message = "Failed to send verification email" });
            }
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    // Request/Response DTOs
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public UserDto? User { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
    }
}