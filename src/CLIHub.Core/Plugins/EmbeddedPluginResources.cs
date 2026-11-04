namespace CLIHub.Core.Plugins;

using System.Reflection;

/// <summary>
///   Enumerates the built-in plugin descriptors and logos embedded in the assembly, so every
///   consumer (seeding, reserved-id checks) works from the same resource set instead of a
///   hardcoded list.
/// </summary>
internal static class EmbeddedPluginResources
{
    private const string ResourceMarker = ".SeedPlugins.";
    private const string DescriptorSuffix = ".plugin.json";
    private const string LogoSuffix = ".logo.png";

    /// <summary> One embedded built-in descriptor resource. </summary>
    /// <param name="Key"> The sanitized resource key linking a logo back to its descriptor. </param>
    /// <param name="ResourceName"> The full manifest resource name. </param>
    /// <param name="Id"> The plugin id from the descriptor content, or null when unreadable. </param>
    /// <param name="Json"> The raw descriptor JSON. </param>
    public sealed record EmbeddedDescriptor(string Key, string ResourceName, string? Id, string Json);

    /// <summary> One embedded built-in logo resource. </summary>
    /// <param name="Key"> The sanitized resource key matching the descriptor's key. </param>
    /// <param name="ResourceName"> The full manifest resource name. </param>
    public sealed record EmbeddedLogo(string Key, string ResourceName);

    /// <summary>
    ///   Enumerates the embedded built-in descriptor resources in manifest order.
    /// </summary>
    /// <param name="assembly"> The assembly holding the SeedPlugins resources. </param>
    /// <returns> The embedded descriptors; malformed entries carry a null id. </returns>
    public static IReadOnlyList<EmbeddedDescriptor> EnumerateDescriptors(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var descriptors = new List<EmbeddedDescriptor>();
        foreach (var name in assembly.GetManifestResourceNames()
                     .Where(n => n.Contains(ResourceMarker, StringComparison.Ordinal))
                     .Where(n => n.EndsWith(DescriptorSuffix, StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            descriptors.Add(new EmbeddedDescriptor(ExtractKey(name, DescriptorSuffix), name, ReadPluginId(json), json));
        }

        return descriptors;
    }

    /// <summary>
    ///   Enumerates the embedded built-in logo resources in manifest order.
    /// </summary>
    /// <param name="assembly"> The assembly holding the SeedPlugins resources. </param>
    /// <returns> The embedded logos. </returns>
    public static IReadOnlyList<EmbeddedLogo> EnumerateLogos(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetManifestResourceNames()
            .Where(n => n.Contains(ResourceMarker, StringComparison.Ordinal))
            .Where(n => n.EndsWith(LogoSuffix, StringComparison.Ordinal))
            .Select(n => new EmbeddedLogo(ExtractKey(n, LogoSuffix), n))
            .ToArray();
    }

    /// <summary>
    ///   Reads the raw bytes of an embedded logo resource.
    /// </summary>
    /// <param name="assembly"> The assembly holding the SeedPlugins resources. </param>
    /// <param name="logo"> The logo resource to read. </param>
    /// <returns> The logo bytes, or null when the resource stream cannot be opened. </returns>
    public static byte[]? ReadLogo(Assembly assembly, EmbeddedLogo logo)
    {
        using var stream = assembly.GetManifestResourceStream(logo.ResourceName);
        if (stream is null)
        {
            return null;
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>
    ///   Gets the ids of every built-in plugin embedded in the assembly. Installed plugins may
    ///   not use any of these ids: the built-in namespace is reserved.
    /// </summary>
    /// <param name="assembly"> The assembly holding the SeedPlugins resources. </param>
    /// <returns> The distinct built-in plugin ids. </returns>
    public static IReadOnlyList<string> BuiltinPluginIds(Assembly assembly) =>
        EnumerateDescriptors(assembly)
            .Where(d => !string.IsNullOrWhiteSpace(d.Id))
            .Select(d => d.Id!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static string ExtractKey(string resourceName, string suffix)
    {
        var start = resourceName.IndexOf(ResourceMarker, StringComparison.Ordinal) + ResourceMarker.Length;
        return resourceName.Substring(start, resourceName.Length - start - suffix.Length);
    }

    private static string? ReadPluginId(string json)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("id", out var id))
            {
                return id.GetString();
            }
        }
        catch
        {
            // malformed seed resource
        }

        return null;
    }
}
