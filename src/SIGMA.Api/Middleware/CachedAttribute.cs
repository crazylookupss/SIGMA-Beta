namespace SIGMA.Api.Middleware;

/// <summary>
/// Endpoint metadata marker that tells the security headers middleware
/// to emit Cache-Control: private, max-age=N instead of no-store.
/// </summary>
public sealed class CachedAttribute(int maxAgeSeconds = 300) : Attribute
{
    public int MaxAgeSeconds { get; } = maxAgeSeconds;
}
