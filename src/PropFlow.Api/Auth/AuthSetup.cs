using System.Security.Claims;
using System.Text;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace PropFlow.Api.Auth;

public static class AuthSetup
{
    /// <summary>
    /// Registers JWT Bearer authentication.
    ///
    /// Expected JWT claims:
    ///   - "tenant_id"  : Guid  — identifies the hotel (Finbuckle resolves the schema from this)
    ///   - "property_id": Guid  — scopes all operations to a single property
    ///   - "role"       : string — "receptionist" | "housekeeper" | "manager" | "service"
    ///
    /// Token issued by the platform identity service (OIDC / Azure AD B2C / Auth0).
    /// API Keys for service-to-service calls are handled by ApiKeyMiddleware.
    /// </summary>
    public static IServiceCollection AddPropFlowAuth(
        this IServiceCollection services,
        IConfiguration config)
    {
        var jwtSection = config.GetSection("Jwt");
        var secret = jwtSection["Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is required.");
        var issuer   = jwtSection["Issuer"]   ?? "propflow";
        var audience = jwtSection["Audience"] ?? "propflow-api";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey  = true,
                    ValidIssuer              = issuer,
                    ValidAudience            = audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                  Encoding.UTF8.GetBytes(secret)),
                    ClockSkew                = TimeSpan.FromSeconds(30),
                };

                opts.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        // Enforce tenant_id claim is present
                        var tenantId = ctx.Principal?.FindFirstValue("tenant_id");
                        if (string.IsNullOrEmpty(tenantId))
                        {
                            ctx.Fail("tenant_id claim is missing from the token.");
                        }
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization(opts =>
        {
            // All endpoints require authentication by default
            opts.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Role-based policies
            opts.AddPolicy("Manager",       p => p.RequireClaim("role", "manager"));
            opts.AddPolicy("Receptionist",  p => p.RequireClaim("role", "receptionist", "manager"));
            opts.AddPolicy("Housekeeper",   p => p.RequireClaim("role", "housekeeper", "manager"));
            opts.AddPolicy("ServiceAccount",p => p.RequireClaim("role", "service"));
        });

        return services;
    }
}
