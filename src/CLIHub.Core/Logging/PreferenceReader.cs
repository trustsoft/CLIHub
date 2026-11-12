namespace CLIHub.Core.Logging;

using System.Text.Json;

/// <summary>
///   Reads individual preference values from config.json without constructing services.
///   Used for bootstrap decisions (for example, the log level) that must precede the DI container.
/// </summary>
public static class PreferenceReader
{
    /// <summary>
    ///   Reads <c> preferences.logLevel </c> from the given config file.
    ///   Returns null when the file, the property, or a usable value is absent.
    /// </summary>
    public static string? ReadLogLevel(string configFilePath) =>
        ReadString(configFilePath, "logLevel");

    /// <summary>
    ///   Reads <c> preferences.logRetentionDays </c> from the given config file.
    ///   Returns null when the file, the property, or a usable value is absent.
    /// </summary>
    public static int? ReadLogRetentionDays(string configFilePath) =>
        ReadInt(configFilePath, "logRetentionDays");

    /// <summary>
    ///   Reads <c> preferences.logFileSizeLimitMb </c> from the given config file.
    ///   Returns null when the file, the property, or a usable value is absent.
    /// </summary>
    public static int? ReadLogFileSizeLimitMb(string configFilePath) =>
        ReadInt(configFilePath, "logFileSizeLimitMb");

    /// <summary>
    ///   Reads <c> preferences.appTheme </c> from the given config file.
    ///   Returns null when the file, the property, or a usable value is absent.
    /// </summary>
    public static string? ReadAppTheme(string configFilePath) =>
        ReadString(configFilePath, "appTheme");

    /// <summary>
    ///   Reads <c> preferences.accentColor </c> from the given config file.
    ///   Returns null when the file, the property, or a usable value is absent.
    /// </summary>
    public static string? ReadAccentColor(string configFilePath) =>
        ReadString(configFilePath, "accentColor");

    private static string? ReadString(string configFilePath, string propertyName) =>
        ReadPreference<string>(configFilePath, propertyName, value => value.GetString());

    private static int? ReadInt(string configFilePath, string propertyName) =>
        ReadPreference<int?>(configFilePath, propertyName, value => value.TryGetInt32(out var result) ? result : null);

    private static T? ReadPreference<T>(string configFilePath, string propertyName, Func<JsonElement, T?> read)
    {
        try
        {
            if (!File.Exists(configFilePath))
            {
                return default;
            }

            using var document = JsonDocument.Parse(File.ReadAllText(configFilePath));
            if (document.RootElement.TryGetProperty("preferences", out var preferences) &&
                preferences.TryGetProperty(propertyName, out var value))
            {
                return read(value);
            }
        }
        catch
        {
            // A malformed config must not prevent startup; fall through to the default.
        }

        return default;
    }
}
