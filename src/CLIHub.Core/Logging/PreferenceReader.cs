namespace CLIHub.Core.Logging;

using System.Text.Json;

/// <summary>
/// Reads individual preference values from config.json without constructing services.
/// Used for bootstrap decisions (for example, the log level) that must precede the DI container.
/// </summary>
public static class PreferenceReader
{
    /// <summary>
    /// Reads <c>preferences.logLevel</c> from the given config file. Returns null when the
    /// file, the property, or a usable value is absent.
    /// </summary>
    public static string? ReadLogLevel(string configFilePath)
    {
        try
        {
            if (!File.Exists(configFilePath))
            {
                return null;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(configFilePath));
            if (document.RootElement.TryGetProperty("preferences", out var preferences) &&
                preferences.TryGetProperty("logLevel", out var logLevel))
            {
                return logLevel.GetString();
            }
        }
        catch
        {
            // A malformed config must not prevent startup; fall through to the default.
        }

        return null;
    }
}
