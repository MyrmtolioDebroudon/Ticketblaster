# TicketBlaster Authentication & Authorization Implementation

## Overview
This implementation provides a complete authentication and authorization system for the TicketBlaster platform using Keycloak as the identity provider with JWT Bearer token authentication.

## Architecture

### Backend Components

#### 1. **Services**
- **KeycloakService** (`TicketBlaster.Server/Services/KeycloakService.cs`)
  - Handles all Keycloak API interactions
  - User management (CRUD operations)
  - Role assignment and management
  - Token generation and validation
  - Password reset functionality

- **UserService** (`TicketBlaster.Server/Services/UserService.cs`)
  - Manages local user database operations
  - Synchronizes with Keycloak
  - Handles user profiles and preferences
  - Manages organizer statistics

#### 2. **Controllers**
- **AuthController** (`TicketBlaster.Server/Controllers/AuthController.cs`)
  - `/api/auth/login` - User login
  - `/api/auth/register` - User registration
  - `/api/auth/refresh` - Token refresh
  - `/api/auth/logout` - User logout
  - `/api/auth/forgot-password` - Password reset
  - `/api/auth/verify-email` - Email verification

- **UserController** (`TicketBlaster.Server/Controllers/UserController.cs`)
  - `/api/user/profile` - Get/Update user profile
  - `/api/user/roles` - Get user roles
  - `/api/user/become-organizer` - Upgrade to organizer
  - Admin endpoints for user management

#### 3. **Configuration** (`Program.cs`)
- JWT Bearer authentication configured
- OpenID Connect for Keycloak integration
- Role-based authorization policies (Admin, Organizer, Customer)
- HTTP client for Keycloak service
- Database seeding for default roles

### Frontend Components

#### 1. **Services**
- **AuthenticationService** (`TicketBlaster.Client/Services/AuthenticationService.cs`)
  - Handles authentication state management
  - Token storage in localStorage
  - API communication for auth operations

- **CustomAuthStateProvider** (`TicketBlaster.Client/Services/CustomAuthStateProvider.cs`)
  - Provides authentication state to Blazor components
  - JWT token parsing and validation
  - Claims extraction from tokens

#### 2. **UI Components**
- **Login Component** (`TicketBlaster.Modules/Authentication/Login.razor`)
  - User login interface
  - Form validation
  - Remember me functionality

- **Register Component** (`TicketBlaster.Modules/Authentication/Register.razor`)
  - User registration with validation
  - Password strength requirements
  - Terms acceptance

- **UserProfile Component** (`TicketBlaster.Modules/Authentication/UserProfile.razor`)
  - Profile management
  - Preference settings
  - Role display
  - Organizer upgrade option

## Security Features

### Authentication
- OAuth 2.0/OpenID Connect via Keycloak
- JWT Bearer tokens for API authentication
- Secure token storage in browser localStorage
- Automatic token refresh
- Session management

### Authorization
- Role-based access control (RBAC)
- Three primary roles: Admin, Organizer, Customer
- Policy-based authorization for API endpoints
- Component-level authorization in UI

### Password Security
- Strong password requirements enforced
- Temporary password generation for resets
- Email verification required

## API Endpoints

### Public Endpoints (No Authentication Required)
- `POST /api/auth/login`
- `POST /api/auth/register`
- `POST /api/auth/forgot-password`
- `POST /api/auth/refresh`

### Protected Endpoints (Authentication Required)
- `GET /api/user/profile`
- `PUT /api/user/profile`
- `GET /api/user/roles`
- `POST /api/user/become-organizer`
- `POST /api/auth/logout`
- `POST /api/auth/verify-email`

### Admin-Only Endpoints
- `GET /api/user/admin/users`
- `GET /api/user/admin/users/{userId}`
- `PUT /api/user/admin/users/{userId}/roles`
- `POST /api/user/admin/users/{userId}/disable`
- `POST /api/user/admin/users/{userId}/enable`
- `DELETE /api/user/admin/users/{userId}`

## Configuration Requirements

### appsettings.json
```json
{
  "Keycloak": {
    "Authority": "https://localhost:8080/auth/realms/ticketblaster",
    "ClientId": "ticketblaster-client",
    "ClientSecret": "your-client-secret",
    "Audience": "ticketblaster-api"
  }
}
```

### Keycloak Setup
1. Create a realm named "ticketblaster"
2. Create a client with the specified ClientId
3. Configure client for confidential access type
4. Enable service accounts
5. Create realm roles: Admin, Organizer, Customer

## Usage Examples

### Login
```csharp
var result = await AuthService.LoginAsync("user@example.com", "password");
if (result.Success)
{
    // User is logged in
    var user = result.User;
    var token = result.AccessToken;
}
```

### Check Authorization
```csharp
@attribute [Authorize(Policy = "OrganizerOnly")]
// or
var isOrganizer = await AuthService.IsInRoleAsync("Organizer");
```

### Update Profile
```csharp
var profile = new UserProfile
{
    FirstName = "John",
    LastName = "Doe",
    // ... other properties
};
await AuthService.UpdateUserProfileAsync(profile);
```

## Next Steps

### To Complete Implementation:
1. Implement EmailService for sending emails
2. Add two-factor authentication support
3. Implement social login providers
4. Add audit logging for security events
5. Create admin UI for user management
6. Add password policy configuration
7. Implement account lockout policies
8. Add CAPTCHA for registration
9. Create email templates

### Testing Required:
1. Unit tests for services
2. Integration tests for API endpoints
3. UI component tests
4. Security penetration testing
5. Load testing for authentication endpoints

## Dependencies
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.AspNetCore.Authentication.OpenIdConnect
- Entity Framework Core
- System.IdentityModel.Tokens.Jwt
- Microsoft.AspNetCore.Components.Authorization