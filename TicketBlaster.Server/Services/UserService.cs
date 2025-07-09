using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketBlaster.Database;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class UserService : IUserService
    {
        private readonly TicketBlasterDbContext _context;
        private readonly IKeycloakService _keycloakService;
        private readonly ILogger<UserService> _logger;

        public UserService(TicketBlasterDbContext context, IKeycloakService keycloakService, ILogger<UserService> logger)
        {
            _context = context;
            _keycloakService = keycloakService;
            _logger = logger;
        }

        public async Task<User?> GetUserAsync(int userId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                return null;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email {Email}", email);
                return null;
            }
        }

        public async Task<User?> GetUserByKeycloakIdAsync(string keycloakId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by Keycloak ID {KeycloakId}", keycloakId);
                return null;
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                // Create user in Keycloak first
                var keycloakRequest = new CreateKeycloakUserRequest
                {
                    Username = user.Email,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Enabled = user.IsActive,
                    EmailVerified = user.IsVerified
                };

                var keycloakUser = await _keycloakService.CreateUserAsync(keycloakRequest);
                user.KeycloakId = keycloakUser.Id;

                // Create user in database
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Assign default role
                await AssignRoleAsync(user.UserId, await GetOrCreateRoleAsync("Customer"));

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            try
            {
                // Update user in Keycloak
                if (!string.IsNullOrEmpty(user.KeycloakId))
                {
                    var keycloakRequest = new UpdateKeycloakUserRequest
                    {
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Enabled = user.IsActive,
                        EmailVerified = user.IsVerified
                    };

                    await _keycloakService.UpdateUserAsync(user.KeycloakId, keycloakRequest);
                }

                // Update user in database
                user.UpdatedOn = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", user.UserId);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await GetUserAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Delete user from Keycloak
                if (!string.IsNullOrEmpty(user.KeycloakId))
                {
                    await _keycloakService.DeleteUserAsync(user.KeycloakId);
                }

                // Delete user from database
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> AssignRoleAsync(int userId, int roleId)
        {
            try
            {
                var user = await GetUserAsync(userId);
                var role = await _context.Roles.FindAsync(roleId);

                if (user == null || role == null)
                {
                    return false;
                }

                // Check if user already has this role
                var existingRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.IsActive);

                if (existingRole != null)
                {
                    return true; // Already has the role
                }

                // Assign role in Keycloak
                if (!string.IsNullOrEmpty(user.KeycloakId))
                {
                    await _keycloakService.AssignRoleAsync(user.KeycloakId, role.Name);
                }

                // Assign role in database
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    EffectiveDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = userId // Self-assigned for now
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
                return false;
            }
        }

        public async Task<bool> RemoveRoleAsync(int userId, int roleId)
        {
            try
            {
                var user = await GetUserAsync(userId);
                var role = await _context.Roles.FindAsync(roleId);

                if (user == null || role == null)
                {
                    return false;
                }

                var userRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.IsActive);

                if (userRole == null)
                {
                    return false; // Doesn't have the role
                }

                // Remove role from Keycloak
                if (!string.IsNullOrEmpty(user.KeycloakId))
                {
                    await _keycloakService.RemoveRoleAsync(user.KeycloakId, role.Name);
                }

                // Remove role from database (soft delete)
                userRole.IsActive = false;
                userRole.ExpiryDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
                return false;
            }
        }

        public async Task<IEnumerable<Role>> GetUserRolesAsync(int userId)
        {
            try
            {
                var userRoles = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == userId && ur.IsActive)
                    .Select(ur => ur.Role)
                    .ToListAsync();

                return userRoles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user {UserId}", userId);
                return new List<Role>();
            }
        }

        public async Task<bool> IsInRoleAsync(int userId, string roleName)
        {
            try
            {
                return await _context.UserRoles
                    .Include(ur => ur.Role)
                    .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName && ur.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role {RoleName} for user {UserId}", roleName, userId);
                return false;
            }
        }

        public async Task<UserProfile> GetUserProfileAsync(int userId)
        {
            try
            {
                var user = await GetUserAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"User {userId} not found");
                }

                return new UserProfile
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth,
                    ProfileImageUrl = user.ProfileImageUrl,
                    Bio = user.Bio,
                    Company = user.Company,
                    JobTitle = user.JobTitle,
                    Website = user.Website,
                    TimeZone = user.TimeZone,
                    Language = user.Language,
                    Currency = user.Currency
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, UserProfile profile)
        {
            try
            {
                var user = await GetUserAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Update profile fields
                user.FirstName = profile.FirstName;
                user.LastName = profile.LastName;
                user.Email = profile.Email;
                user.PhoneNumber = profile.PhoneNumber;
                user.DateOfBirth = profile.DateOfBirth;
                user.ProfileImageUrl = profile.ProfileImageUrl;
                user.Bio = profile.Bio;
                user.Company = profile.Company;
                user.JobTitle = profile.JobTitle;
                user.Website = profile.Website;
                user.TimeZone = profile.TimeZone;
                user.Language = profile.Language;
                user.Currency = profile.Currency;

                await UpdateUserAsync(user);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> VerifyUserAsync(int userId)
        {
            try
            {
                var user = await GetUserAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.IsVerified = true;
                await UpdateUserAsync(user);

                // Also verify in Keycloak
                if (!string.IsNullOrEmpty(user.KeycloakId))
                {
                    await _keycloakService.VerifyEmailAsync(user.KeycloakId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying user {UserId}", userId);
                return false;
            }
        }

        public async Task<IEnumerable<User>> GetOrganizersAsync(int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                return await _context.Users
                    .Where(u => u.IsOrganizer && u.IsActive)
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organizers");
                return new List<User>();
            }
        }

        public async Task<OrganizerStats> GetOrganizerStatsAsync(int userId)
        {
            try
            {
                var user = await GetUserAsync(userId);
                if (user == null || !user.IsOrganizer)
                {
                    throw new InvalidOperationException($"User {userId} is not an organizer");
                }

                var events = await _context.Events
                    .Include(e => e.TicketTypes)
                    .ThenInclude(tt => tt.OrderItems)
                    .Where(e => e.OrganizerId == userId)
                    .ToListAsync();

                var stats = new OrganizerStats
                {
                    TotalEvents = events.Count,
                    ActiveEvents = events.Count(e => e.Status == EventStatus.Active),
                    CompletedEvents = events.Count(e => e.Status == EventStatus.Completed),
                    TotalTicketsSold = events.SelectMany(e => e.TicketTypes)
                        .SelectMany(tt => tt.OrderItems)
                        .Sum(oi => oi.Quantity),
                    TotalRevenue = events.SelectMany(e => e.TicketTypes)
                        .SelectMany(tt => tt.OrderItems)
                        .Sum(oi => oi.TotalPrice),
                    TotalAttendees = events.SelectMany(e => e.TicketTypes)
                        .SelectMany(tt => tt.OrderItems)
                        .Select(oi => oi.Order.UserId)
                        .Distinct()
                        .Count(),
                    LastEventDate = events.Where(e => e.EndDateTime < DateTime.UtcNow)
                        .OrderByDescending(e => e.EndDateTime)
                        .FirstOrDefault()?.EndDateTime,
                    NextEventDate = events.Where(e => e.StartDateTime > DateTime.UtcNow)
                        .OrderBy(e => e.StartDateTime)
                        .FirstOrDefault()?.StartDateTime
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting organizer stats for user {UserId}", userId);
                throw;
            }
        }

        private async Task<int> GetOrCreateRoleAsync(string roleName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                role = new Role
                {
                    Name = roleName,
                    Description = $"{roleName} role",
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow
                };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }
            return role.RoleId;
        }
    }
}