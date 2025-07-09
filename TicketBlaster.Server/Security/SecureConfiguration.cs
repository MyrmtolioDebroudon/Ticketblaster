using Microsoft.Extensions.Configuration;
using System;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Azure.Identity;

namespace TicketBlaster.Server.Security
{
    public static class SecureConfiguration
    {
        public static string GetSecureValue(IConfiguration configuration, string key, string environmentVariable = null)
        {
            // Priority: Environment Variable > User Secrets > Configuration
            var envVar = environmentVariable ?? key.Replace(":", "_").ToUpper();
            var value = Environment.GetEnvironmentVariable(envVar);
            
            if (string.IsNullOrEmpty(value))
            {
                value = configuration[key];
            }
            
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"Required configuration value '{key}' is missing. Please set it in environment variables or user secrets.");
            }
            
            return value;
        }

        public static bool IsProduction(IHostEnvironment environment)
        {
            return environment.IsProduction() || 
                   Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";
        }

        public static void ValidateSecurityConfiguration(IConfiguration configuration, IHostEnvironment environment)
        {
            var errors = new List<string>();

            // Check for insecure configurations in production
            if (IsProduction(environment))
            {
                // Ensure HTTPS is required
                var httpsPort = configuration["ASPNETCORE_HTTPS_PORT"];
                if (string.IsNullOrEmpty(httpsPort))
                {
                    errors.Add("HTTPS port not configured for production environment");
                }

                // Check for default or weak secrets
                var keycloakSecret = configuration["Keycloak:ClientSecret"];
                if (keycloakSecret == "your-client-secret" || keycloakSecret?.Length < 32)
                {
                    errors.Add("Keycloak client secret is using default or weak value");
                }

                var stripeSecret = configuration["Stripe:SecretKey"];
                if (stripeSecret?.StartsWith("sk_test_") == true)
                {
                    errors.Add("Stripe is using test keys in production");
                }

                // Ensure database connection is encrypted
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrEmpty(connectionString) && 
                    !connectionString.Contains("Encrypt=True", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Database connection is not encrypted");
                }
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(
                    "Security configuration errors detected:\n" + string.Join("\n", errors));
            }
        }

        public static IConfigurationBuilder AddSecureConfiguration(
            this IConfigurationBuilder builder, 
            IHostEnvironment environment)
        {
            // Add configuration sources in order of precedence
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

            // Add user secrets in development
            if (environment.IsDevelopment())
            {
                builder.AddUserSecrets<Program>();
            }

            // Environment variables override everything
            builder.AddEnvironmentVariables();

            // Add Azure Key Vault if configured
            var config = builder.Build();
            var keyVaultEndpoint = config["AzureKeyVault:Endpoint"];
            if (!string.IsNullOrEmpty(keyVaultEndpoint))
            {
                var keyVaultClientId = config["AzureKeyVault:ClientId"];
                var keyVaultClientSecret = config["AzureKeyVault:ClientSecret"];
                
                if (!string.IsNullOrEmpty(keyVaultClientId) && !string.IsNullOrEmpty(keyVaultClientSecret))
                {
                    // Note: In production, use Managed Identity instead of client credentials
                    builder.AddAzureKeyVault(
                        new Uri(keyVaultEndpoint),
                        new Azure.Identity.ClientSecretCredential(
                            config["AzureKeyVault:TenantId"],
                            keyVaultClientId,
                            keyVaultClientSecret));
                }
            }

            return builder;
        }
    }
}