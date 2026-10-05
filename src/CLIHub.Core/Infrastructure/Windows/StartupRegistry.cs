namespace CLIHub.Core.Infrastructure.Windows;

/// <summary>
///   Thin seam over the current user's Run registry key, so startup registration is testable.
/// </summary>
internal interface IStartupRegistry
{
    string? GetValue(string name);

    void SetValue(string name, string value);

    void DeleteValue(string name);
}

/// <summary>
///   Default <see cref="IStartupRegistry"/> backed by
///   <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c>.
/// </summary>
internal sealed class CurrentUserRegistryStartup : IStartupRegistry
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public string? GetValue(string name)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(name) as string;
    }

    public void SetValue(string name, string value)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        key.SetValue(name, value);
    }

    public void DeleteValue(string name)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
