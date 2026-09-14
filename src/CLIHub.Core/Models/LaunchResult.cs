namespace CLIHub.Core.Models;

public sealed record LaunchResult(bool Success, string Runtime, string? Warning, string? Error)
{
    public static LaunchResult Ok(string runtime, string? warning = null) => new(true, runtime, warning, null);

    public static LaunchResult Failed(string error) => new(false, string.Empty, null, error);
}
