namespace CLIHub.Core.Infrastructure.Persistence;

/// <summary>
///   The single fallback-logo configuration shared by every logo resolver: the path used when
///   neither the cache nor the file system yields a logo.
/// </summary>
/// <param name="DefaultLogoPath"> The absolute path to the default logo asset, or null. </param>
public sealed record LogoOptions(string? DefaultLogoPath);
