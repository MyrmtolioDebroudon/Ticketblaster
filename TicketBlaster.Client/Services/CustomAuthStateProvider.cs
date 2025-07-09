using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace TicketBlaster.Client.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _httpClient;
        private AuthenticationState _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        public CustomAuthStateProvider(IJSRuntime jsRuntime, HttpClient httpClient)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
                
                if (string.IsNullOrEmpty(token))
                {
                    return _anonymous;
                }

                // Parse JWT token
                var claims = ParseClaimsFromJwt(token);
                var expiry = claims.FirstOrDefault(c => c.Type == "exp");
                
                if (expiry != null)
                {
                    var expiryDateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiry.Value));
                    if (expiryDateTime <= DateTimeOffset.UtcNow)
                    {
                        // Token has expired
                        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                        return _anonymous;
                    }
                }

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error getting authentication state: {ex.Message}");
                return _anonymous;
            }
        }

        public void NotifyUserAuthentication(string token)
        {
            var claims = ParseClaimsFromJwt(token);
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
            NotifyAuthenticationStateChanged(authState);
        }

        public void NotifyUserLogout()
        {
            var authState = Task.FromResult(_anonymous);
            NotifyAuthenticationStateChanged(authState);
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];

            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs == null) return claims;

            // Extract standard claims
            if (keyValuePairs.TryGetValue("sub", out var sub))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.ToString() ?? string.Empty));

            if (keyValuePairs.TryGetValue("email", out var email))
                claims.Add(new Claim(ClaimTypes.Email, email.ToString() ?? string.Empty));

            if (keyValuePairs.TryGetValue("name", out var name))
                claims.Add(new Claim(ClaimTypes.Name, name.ToString() ?? string.Empty));

            if (keyValuePairs.TryGetValue("given_name", out var givenName))
                claims.Add(new Claim(ClaimTypes.GivenName, givenName.ToString() ?? string.Empty));

            if (keyValuePairs.TryGetValue("family_name", out var familyName))
                claims.Add(new Claim(ClaimTypes.Surname, familyName.ToString() ?? string.Empty));

            // Extract roles
            if (keyValuePairs.TryGetValue("realm_access", out var realmAccess))
            {
                var realmAccessDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(realmAccess.ToString() ?? "{}");
                if (realmAccessDict != null && realmAccessDict.TryGetValue("roles", out var rolesElement))
                {
                    var roles = rolesElement.EnumerateArray();
                    foreach (var role in roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));
                    }
                }
            }

            // Extract custom claims
            if (keyValuePairs.TryGetValue("preferred_username", out var preferredUsername))
                claims.Add(new Claim("preferred_username", preferredUsername.ToString() ?? string.Empty));

            if (keyValuePairs.TryGetValue("exp", out var exp))
                claims.Add(new Claim("exp", exp.ToString() ?? string.Empty));

            if (keyValuePairs.TryGetValue("iat", out var iat))
                claims.Add(new Claim("iat", iat.ToString() ?? string.Empty));

            // Add any additional claims
            foreach (var kvp in keyValuePairs)
            {
                if (!claims.Any(c => c.Type == kvp.Key))
                {
                    var value = kvp.Value?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(value))
                    {
                        claims.Add(new Claim(kvp.Key, value));
                    }
                }
            }

            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}