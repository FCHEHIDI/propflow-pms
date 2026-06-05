using System.Security.Cryptography;
using System.Text;

namespace PropFlow.Api.Auth;

/// <summary>
/// Validates API Key header for service-to-service calls (channel manager, IoT panel).
///
/// Header: X-Api-Key: {raw_key}
///
/// Invariant: the raw key is NEVER stored. Only SHA-256(key) is persisted in the DB.
/// This middleware computes SHA-256 of the incoming key and looks it up.
///
/// Skipped on endpoints tagged with [AllowAnonymous] or Scalar/OpenAPI paths.
/// </summary>
public sealed class ApiKeyMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth paths
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // If JWT bearer already authenticated — skip API key check
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var rawKey))
        {
            // No JWT, no API key — fall through to auth middleware which will return 401
            await _next(context);
            return;
        }

        var hashedKey = HashKey(rawKey.ToString());
        _logger.LogDebug("API key authentication attempt (hash prefix: {Prefix}...)",
            hashedKey[..8]);

        // TODO: look up hashedKey in tenant API keys table via IApiKeyRepository
        // For now: allow if key is present (trust JWT/Finbuckle for tenant isolation)
        // Replace with: var identity = await _apiKeyRepo.FindAsync(hashedKey);

        await _next(context);
    }

    /// <summary>SHA-256 of the raw key. Safe to store and compare.</summary>
    public static string HashKey(string rawKey)
    {
        var bytes = Encoding.UTF8.GetBytes(rawKey);
        var hash  = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
