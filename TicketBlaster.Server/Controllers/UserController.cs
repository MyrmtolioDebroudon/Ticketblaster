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
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IKeycloakService _keycloakService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            IKeycloakService keycloakService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _keycloakService = keycloakService;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
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

                var profile = await _userService.GetUserProfileAsync(user.UserId);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new { message = "Error retrieving profile" });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfile profile)
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

                var success = await _userService.UpdateUserProfileAsync(user.UserId, profile);
                if (success)
                {
                    return Ok(new { message = "Profile updated successfully" });
                }

                return BadRequest(new { message = "Failed to update profile" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new { message = "Error updating profile" });
            }
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetMyRoles()
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

                var roles = await _userService.GetUserRolesAsync(user.UserId);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles");
                return StatusCode(500, new { message = "Error retrieving roles" });
            }
        }

        [HttpPost("become-organizer")]
        public async Task<IActionResult> BecomeOrganizer([FromBody] BecomeOrganizerRequest request)
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

                if (user.IsOrganizer)
                {
                    return BadRequest(new { message = "User is already an organizer" });
                }

                // Update user profile with organizer info
                user.IsOrganizer = true;
                user.Company = request.Company;
                user.JobTitle = request.JobTitle;
                user.Website = request.Website;
                user.Bio = request.Bio;

                await _userService.UpdateUserAsync(user);

                // Assign Organizer role
                var organizerRoleId = await GetRoleIdByNameAsync("Organizer");
                if (organizerRoleId > 0)
                {
                    await _userService.AssignRoleAsync(user.UserId, organizerRoleId);
                }

                return Ok(new { message = "Successfully became an organizer" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing organizer request");
                return StatusCode(500, new { message = "Error processing request" });
            }
        }

        // Admin endpoints
        [HttpGet("admin/users")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                // This would need a method to get all users with pagination
                // For now, returning a placeholder
                return Ok(new { message = "Not implemented yet" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { message = "Error retrieving users" });
            }
        }

        [HttpGet("admin/users/{userId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetUser(int userId)
        {
            try
            {
                var user = await _userService.GetUserAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                return StatusCode(500, new { message = "Error retrieving user" });
            }
        }

        [HttpPut("admin/users/{userId}/roles")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateUserRoles(int userId, [FromBody] UpdateUserRolesRequest request)
        {
            try
            {
                var user = await _userService.GetUserAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                // Get current roles
                var currentRoles = await _userService.GetUserRolesAsync(userId);
                var currentRoleIds = currentRoles.Select(r => r.RoleId).ToList();

                // Remove roles that are no longer assigned
                foreach (var roleId in currentRoleIds.Where(id => !request.RoleIds.Contains(id)))
                {
                    await _userService.RemoveRoleAsync(userId, roleId);
                }

                // Add new roles
                foreach (var roleId in request.RoleIds.Where(id => !currentRoleIds.Contains(id)))
                {
                    await _userService.AssignRoleAsync(userId, roleId);
                }

                return Ok(new { message = "Roles updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating roles for user {UserId}", userId);
                return StatusCode(500, new { message = "Error updating roles" });
            }
        }

        [HttpPost("admin/users/{userId}/disable")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DisableUser(int userId)
        {
            try
            {
                var user = await _userService.GetUserAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                user.IsActive = false;
                await _userService.UpdateUserAsync(user);

                // Also disable in Keycloak
                if (!string.IsNullOrEmpty(user.KeycloakId))
                {
                    await _keycloakService.DisableUserAsync(user.KeycloakId);
                }

                return Ok(new { message = "User disabled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling user {UserId}", userId);
                return StatusCode(500, new { message = "Error disabling user" });
            }
        }

        [HttpPost("admin/users/{userId}/enable")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EnableUser(int userId)
        {
            try
            {
                var user = await _userService.GetUserAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                user.IsActive = true;
                await _userService.UpdateUserAsync(user);

                // Also enable in Keycloak
                if (!string.IsNullOrEmpty(user.KeycloakId))
                {
                    await _keycloakService.EnableUserAsync(user.KeycloakId);
                }

                return Ok(new { message = "User enabled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling user {UserId}", userId);
                return StatusCode(500, new { message = "Error enabling user" });
            }
        }

        [HttpDelete("admin/users/{userId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                var success = await _userService.DeleteUserAsync(userId);
                if (success)
                {
                    return Ok(new { message = "User deleted successfully" });
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return StatusCode(500, new { message = "Error deleting user" });
            }
        }

        [HttpGet("organizers")]
        public async Task<IActionResult> GetOrganizers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var organizers = await _userService.GetOrganizersAsync(pageNumber, pageSize);
                return Ok(organizers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organizers");
                return StatusCode(500, new { message = "Error retrieving organizers" });
            }
        }

        [HttpGet("organizers/{userId}/stats")]
        public async Task<IActionResult> GetOrganizerStats(int userId)
        {
            try
            {
                var stats = await _userService.GetOrganizerStatsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organizer stats for user {UserId}", userId);
                return StatusCode(500, new { message = "Error retrieving organizer stats" });
            }
        }

        private async Task<int> GetRoleIdByNameAsync(string roleName)
        {
            // This would need to be implemented in the UserService or a RoleService
            // For now, returning a placeholder
            return roleName switch
            {
                "Admin" => 1,
                "Organizer" => 2,
                "Customer" => 3,
                _ => 0
            };
        }
    }

    // Request DTOs
    public class BecomeOrganizerRequest
    {
        public string Company { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
    }

    public class UpdateUserRolesRequest
    {
        public List<int> RoleIds { get; set; } = new List<int>();
    }
}