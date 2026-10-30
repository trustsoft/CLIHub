namespace CLIHub.Tests.Themes;

using System.Text;
using System.Text.RegularExpressions;

using static CLIHub.Tests.Support.TestHelpers;

/// <summary>
///   Guards the application-level resource contract: palette brushes must be looked up dynamically,
///   the runtime-provided accent keys must stay out of the palettes, and the non-palette part of the
///   merge tree must declare every key exactly once.
/// </summary>
/// <remarks>
///   <para>
///     A palette is merged at runtime on top of the dark base, so a <c>StaticResource</c> reference to
///     a palette brush freezes whichever palette happened to be loaded at parse time. Palette brushes
///     therefore have to be consumed with <c>DynamicResource</c>.
///   </para>
///   <para>
///     The palettes deliberately redefine the same keys and are merged conditionally, so they are
///     excluded from the duplicate check. The walk still visits them, and the test asserts it visited
///     at least six files including a palette, so the exclusion cannot hide a broken walk.
///   </para>
/// </remarks>
public sealed class ApplicationResourceKeyTests
{
    private static readonly Regex DeclaredKeyPattern = new(
        "x:Key=\"(?<key>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MergedSourcePattern = new(
        "Source=\"(?<source>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex StaticResourcePattern = new(
        "\\{StaticResource\\s+(?<key>[^\\s},]+)\\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DynamicResourcePattern = new(
        "\\{DynamicResource\\s+(?<key>[^\\s},]+)\\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // The accent is application-scoped: these keys are written into Application.Resources at
    // runtime by AccentApplier, so no palette may declare them and no XAML may freeze them with a
    // StaticResource lookup.
    private static readonly string[] RuntimeAccentKeys = ["AccentBrush", "AccentSelectionBrush"];

    [Fact]
    public void PaletteBrushKeys_AreNeverReferencedWithStaticResource()
    {
        var appRoot = Path.Combine(FindRepositoryRoot(), "src", "CLIHub");
        var themesDirectory = Path.Combine(appRoot, "Themes");
        var paletteFiles = Directory.GetFiles(themesDirectory, "*Theme.xaml")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            paletteFiles.Length >= 3,
            $"Expected the registered palettes under '{themesDirectory}', but found {paletteFiles.Length}.");

        var paletteKeys = paletteFiles
            .SelectMany(path => ReadDeclaredKeys(File.ReadAllText(path)))
            .ToHashSet(StringComparer.Ordinal);

        // Loud failure on a broken palette glob: an empty key set would make every scan a no-op.
        Assert.NotEmpty(paletteKeys);

        var failures = new List<string>();
        var staticReferenceCount = 0;

        foreach (var path in Directory.GetFiles(appRoot, "*.xaml", SearchOption.AllDirectories)
                     .Where(path => !IsBuildOutput(path))
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var relativePath = Path.GetRelativePath(appRoot, path);
            var lines = File.ReadAllLines(path);

            for (var index = 0; index < lines.Length; index++)
            {
                foreach (Match match in StaticResourcePattern.Matches(lines[index]))
                {
                    staticReferenceCount++;
                    var key = match.Groups["key"].Value;
                    if (paletteKeys.Contains(key))
                    {
                        failures.Add(
                            $"{relativePath}:{index + 1}: StaticResource '{key}' refers to a palette brush; " +
                            "use DynamicResource so the runtime palette is not frozen at parse time.");
                    }
                }
            }
        }

        Assert.True(
            staticReferenceCount > 0,
            "No StaticResource reference was found; the XAML walk is broken.");

        if (failures.Count > 0)
        {
            var report = new StringBuilder();
            report.AppendLine($"{failures.Count} palette brush(es) referenced with StaticResource:");
            foreach (var failure in failures.OrderBy(failure => failure, StringComparer.Ordinal))
            {
                report.AppendLine(failure);
            }

            Assert.Fail(report.ToString());
        }
    }

    [Fact]
    public void RuntimeProvidedAccentKeys_AreNeverDeclaredByAPaletteOrReadWithStaticResource()
    {
        var appRoot = Path.Combine(FindRepositoryRoot(), "src", "CLIHub");
        var themesDirectory = Path.Combine(appRoot, "Themes");
        var paletteFiles = Directory.GetFiles(themesDirectory, "*Theme.xaml")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            paletteFiles.Length >= 3,
            $"Expected the registered palettes under '{themesDirectory}', but found {paletteFiles.Length}.");

        var failures = new List<string>();

        foreach (var path in paletteFiles)
        {
            var declaredKeys = ReadDeclaredKeys(File.ReadAllText(path)).ToHashSet(StringComparer.Ordinal);
            foreach (var key in RuntimeAccentKeys)
            {
                if (declaredKeys.Contains(key))
                {
                    failures.Add(
                        $"{Path.GetRelativePath(appRoot, path)}: palette declares the application-scoped key " +
                        $"'{key}'; the accent is supplied at runtime and independent of the theme.");
                }
            }
        }

        var xamlFiles = Directory.GetFiles(appRoot, "*.xaml", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        // Loud failure on a broken glob: an empty walk would make the StaticResource scan a no-op.
        Assert.True(
            xamlFiles.Length >= 20,
            $"Expected the full XAML tree under '{appRoot}', but found only {xamlFiles.Length} files.");

        var staticReferenceCount = 0;
        var accentReferenceCount = 0;

        foreach (var path in xamlFiles)
        {
            var relativePath = Path.GetRelativePath(appRoot, path);
            var lines = File.ReadAllLines(path);

            for (var index = 0; index < lines.Length; index++)
            {
                foreach (Match match in StaticResourcePattern.Matches(lines[index]))
                {
                    staticReferenceCount++;
                    var key = match.Groups["key"].Value;
                    if (RuntimeAccentKeys.Contains(key, StringComparer.Ordinal))
                    {
                        failures.Add(
                            $"{relativePath}:{index + 1}: StaticResource '{key}' refers to a runtime-provided " +
                            "accent brush; use DynamicResource so the applied accent is not frozen at parse time.");
                    }
                }

                accentReferenceCount += DynamicResourcePattern.Matches(lines[index])
                    .Count(match => RuntimeAccentKeys.Contains(match.Groups["key"].Value, StringComparer.Ordinal));
            }
        }

        Assert.True(
            staticReferenceCount > 0,
            "No StaticResource reference was found; the XAML walk is broken.");
        Assert.True(
            accentReferenceCount > 0,
            "No DynamicResource reference to an accent brush was found; the accent scan is broken.");

        if (failures.Count > 0)
        {
            var report = new StringBuilder();
            report.AppendLine($"{failures.Count} application-scoped accent key problem(s):");
            foreach (var failure in failures.OrderBy(failure => failure, StringComparer.Ordinal))
            {
                report.AppendLine(failure);
            }

            Assert.Fail(report.ToString());
        }
    }

    [Fact]
    public void ApplicationLevelMergedDictionaries_DoNotDuplicateNonPaletteKeys()
    {
        var appRoot = Path.Combine(FindRepositoryRoot(), "src", "CLIHub");
        var appPath = Path.Combine(appRoot, "App.xaml");
        Assert.True(File.Exists(appPath), $"Expected the application XAML at '{appPath}'.");

        var visitedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paletteFileCount = 0;
        var declarations = new Dictionary<string, string>(StringComparer.Ordinal);
        var failures = new List<string>();
        var pending = new Stack<string>();
        pending.Push(appPath);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!visitedFiles.Add(current))
            {
                continue;
            }

            var isPalette = IsPaletteFile(current);
            if (isPalette)
            {
                paletteFileCount++;
            }

            var text = File.ReadAllText(current);
            if (!isPalette)
            {
                foreach (var key in ReadDeclaredKeys(text))
                {
                    if (declarations.TryGetValue(key, out var firstFile))
                    {
                        failures.Add(
                            $"x:Key '{key}' is declared in both '{Path.GetRelativePath(appRoot, firstFile)}' " +
                            $"and '{Path.GetRelativePath(appRoot, current)}'.");
                        continue;
                    }

                    declarations.Add(key, current);
                }
            }

            foreach (Match match in MergedSourcePattern.Matches(text))
            {
                var source = match.Groups["source"].Value.TrimStart('/', '\\');
                var mergedPath = Path.Combine(appRoot, source.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(mergedPath))
                {
                    pending.Push(mergedPath);
                }
                else
                {
                    failures.Add(
                        $"{Path.GetRelativePath(appRoot, current)}: merged dictionary " +
                        $"Source=\"{match.Groups["source"].Value}\" does not resolve inside '{appRoot}'.");
                }
            }
        }

        // Loud failure on a broken walk: the exclusion of the palettes is only sound when the walk
        // actually visited the whole application-level tree.
        Assert.True(
            visitedFiles.Count >= 6,
            $"Expected at least six files in the application-level merge tree, but visited {visitedFiles.Count}.");
        Assert.True(
            paletteFileCount >= 1,
            "No palette file was visited by the application-level merge walk.");
        Assert.NotEmpty(declarations);

        if (failures.Count > 0)
        {
            var report = new StringBuilder();
            report.AppendLine($"{failures.Count} duplicate or unresolved application-level resource problem(s):");
            foreach (var failure in failures.OrderBy(failure => failure, StringComparer.Ordinal))
            {
                report.AppendLine(failure);
            }

            Assert.Fail(report.ToString());
        }
    }

    private static IEnumerable<string> ReadDeclaredKeys(string text)
    {
        return DeclaredKeyPattern.Matches(text).Select(match => match.Groups["key"].Value);
    }

    private static bool IsPaletteFile(string path)
    {
        return Regex.IsMatch(Path.GetFileName(path), "^[^/\\\\]+Theme\\.xaml$", RegexOptions.IgnoreCase);
    }

    private static bool IsBuildOutput(string path)
    {
        var segments = path.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
        return segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
            || segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }
}
