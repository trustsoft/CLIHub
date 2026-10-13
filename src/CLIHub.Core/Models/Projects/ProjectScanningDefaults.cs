namespace CLIHub.Core.Models;

/// <summary>
///   Built-in defaults and bounds for project scanning.
/// </summary>
public static class ProjectScanningDefaults
{
    /// <summary>
    ///   The scan depth used when none is stored.
    /// </summary>
    public const int DefaultScanDepth = 2;

    /// <summary>
    ///   The smallest allowed scan depth.
    /// </summary>
    public const int MinScanDepth = 1;

    /// <summary>
    ///   The largest allowed scan depth.
    /// </summary>
    public const int MaxScanDepth = 8;

    /// <summary>
    ///   The directory-name patterns skipped when the stored pattern list is absent.
    /// </summary>
    public static IReadOnlyList<string> IgnoredPatterns { get; } = new[]
    {
        "node_modules",
        "bin",
        "obj",
        "dist",
        "build",
        "out",
        "target",
        ".vs",
        ".idea",
        ".vscode",
        "packages",
        "vendor",
        "__pycache__",
        ".venv",
        "venv"
    };

    /// <summary>
    ///   Clamps a scan depth into the allowed range.
    /// </summary>
    /// <param name="depth"> The requested depth. </param>
    /// <returns> The clamped depth. </returns>
    public static int NormalizeScanDepth(int depth) => Math.Clamp(depth, MinScanDepth, MaxScanDepth);
}
