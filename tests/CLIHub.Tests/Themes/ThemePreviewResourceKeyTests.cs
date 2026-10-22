namespace CLIHub.Tests.Themes;

using System.Text.RegularExpressions;

using CLIHub.Themes;

/// <summary>
///   Guards the theme preview swatch keys that are referenced <em>by name</em> rather than through a
///   XAML resource expression: the card and bar keys live on <see cref="ThemeDescriptor" /> as strings,
///   and the System dot key is written as a <c> StaticResource </c> in the Appearance page.
/// </summary>
/// <remarks>
///   <para>
///     <see cref="XamlResourceReferenceTests" /> cannot see a preview key that appears only as a C#
///     string literal, so a typo there would surface as a blank swatch at runtime. This test reads the
///     descriptor values themselves (no string scraping of the C# sources, which would miss a typo that
///     breaks the expected name shape) and resolves them against the keys declared in App.xaml and its
///     transitive merges — the scope the Appearance page actually binds against.
///   </para>
///   <para>
///     The check runs both ways: a referenced key that is not declared fails, and a declared
///     <c> ThemePreview </c> key that nothing references fails as an orphan.
///   </para>
/// </remarks>
public sealed class ThemePreviewResourceKeyTests
{
    private const string PreviewKeyPrefix = "ThemePreview";

    private static readonly Regex DeclaredKeyPattern = new(
        "x:Key=\"(?<key>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MergedSourcePattern = new(
        "Source=\"(?<source>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex XamlReferencePattern = new(
        @"\{(?:Static|Dynamic)Resource\s+(?<key>ThemePreview[^\s},]+)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void PreviewBrushKeys_ReferencedByName_ResolveAndLeaveNoOrphans()
    {
        var appRoot = Path.Combine(FindRepositoryRoot(), "src", "CLIHub");
        var appPath = Path.Combine(appRoot, "App.xaml");
        Assert.True(File.Exists(appPath), $"Expected the application XAML at '{appPath}'.");

        var declaredKeys = ReadApplicationLevelKeys(appPath, appRoot);
        var declaredPreviewKeys = declaredKeys
            .Where(key => key.StartsWith(PreviewKeyPrefix, StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        var descriptorKeys = ThemeDescriptors.All
            .SelectMany(descriptor => new[] { descriptor.CardBrush, descriptor.BarBrush })
            .ToHashSet(StringComparer.Ordinal);

        var xamlKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.GetFiles(appRoot, "*.xaml", SearchOption.AllDirectories)
                     .Where(path => !IsBuildOutput(path)))
        {
            foreach (Match match in XamlReferencePattern.Matches(File.ReadAllText(path)))
            {
                xamlKeys.Add(match.Groups["key"].Value);
            }
        }

        // Loud failure on a walk or selection accident: every side must be populated for the check to
        // mean anything.
        Assert.True(declaredPreviewKeys.Count > 0, "No ThemePreview keys were read from the application resources.");
        Assert.NotEmpty(descriptorKeys);
        Assert.NotEmpty(xamlKeys);

        var referencedKeys = descriptorKeys.Concat(xamlKeys).ToHashSet(StringComparer.Ordinal);
        var missing = referencedKeys.Except(declaredPreviewKeys, StringComparer.Ordinal).OrderBy(key => key).ToArray();
        var orphans = declaredPreviewKeys.Except(referencedKeys, StringComparer.Ordinal).OrderBy(key => key).ToArray();

        Assert.True(
            missing.Length == 0 && orphans.Length == 0,
            "Theme preview brush keys disagree with the declared application resources. "
            + $"Referenced but not declared: [{string.Join(", ", missing)}]. "
            + $"Declared but never referenced: [{string.Join(", ", orphans)}].");
    }

    private static HashSet<string> ReadApplicationLevelKeys(string appPath, string appRoot)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>();
        pending.Push(appPath);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visited.Add(current))
            {
                continue;
            }

            var text = File.ReadAllText(current);
            foreach (Match match in DeclaredKeyPattern.Matches(text))
            {
                keys.Add(match.Groups["key"].Value);
            }

            foreach (Match match in MergedSourcePattern.Matches(text))
            {
                var source = match.Groups["source"].Value.TrimStart('/', '\\');
                var mergedPath = Path.Combine(appRoot, source.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(mergedPath))
                {
                    pending.Push(mergedPath);
                }
            }
        }

        return keys;
    }

    private static bool IsBuildOutput(string path)
    {
        var segments = path.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
        return segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
            || segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CLIHub.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
