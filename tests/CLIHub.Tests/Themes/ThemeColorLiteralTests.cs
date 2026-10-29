namespace CLIHub.Tests.Themes;

using System.Text;
using System.Text.RegularExpressions;

/// <summary>
///   Guards the palette as the single source of application colour: a colour literal outside the
///   palette dictionaries silently escapes theming and breaks the next theme change.
/// </summary>
/// <remarks>
///   <para>
///     Two kinds of file are allowlisted by design: the palettes (<c>Themes/*Theme.xaml</c>, colours only)
///     and <c>Themes/ThemePickerData.xaml</c> (theme-identity preview swatches that must read the same
///     under every theme).
///   </para>
///   <para>
///     The walk and the allowlisted literal count are asserted, so a broken glob or a renamed
///     allowlisted file fails loudly instead of passing with zero hits.
///   </para>
/// </remarks>
public sealed class ThemeColorLiteralTests
{
    private static readonly Regex ColorLiteralPattern = new(
        "(?<attribute>Color|Background|Foreground|Fill|Stroke)\\s*=\\s*\"(?<literal>#[0-9A-Fa-f]{6,8})\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // The dedicated SolidColorBrush form is already covered by the attribute pattern above; it is
    // kept as an explicit second pattern so the guard documents the palette shape it protects.
    private static readonly Regex SolidColorBrushLiteralPattern = new(
        "<SolidColorBrush\\b[^>]*?\\bColor\\s*=\\s*\"(?<literal>#[0-9A-Fa-f]{6,8})\"",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void XamlColorLiterals_OutsidePaletteAndThemeDataFiles_AreRejected()
    {
        var appRoot = Path.Combine(FindRepositoryRoot(), "src", "CLIHub");
        var xamlFiles = Directory.GetFiles(appRoot, "*.xaml", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        // Loud failure on glob or rename accidents: the walk is only meaningful when it sees the
        // known application XAML tree and the allowlisted files it is meant to skip.
        Assert.True(
            xamlFiles.Length >= 20,
            $"Expected the full XAML tree under '{appRoot}', but found only {xamlFiles.Length} files.");
        Assert.Contains(xamlFiles, path => IsFile(path, appRoot, "Themes", "ThemePickerData.xaml"));

        var failures = new List<string>();
        var allowlistedLiteralCount = 0;

        foreach (var path in xamlFiles)
        {
            var relativePath = RelativePath(appRoot, path);
            var isAllowlisted = IsAllowlisted(relativePath);
            var lines = File.ReadAllLines(path);

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                var matches = ColorLiteralPattern.Matches(line)
                    .Concat(SolidColorBrushLiteralPattern.Matches(line))
                    .ToArray();
                if (matches.Length == 0)
                {
                    continue;
                }

                var literal = matches[0].Groups["literal"].Value;
                if (isAllowlisted)
                {
                    allowlistedLiteralCount++;
                    continue;
                }

                failures.Add(
                    $"{relativePath}:{index + 1}: colour literal {literal} outside the palette allowlist.");
            }
        }

        Assert.True(
            allowlistedLiteralCount > 0,
            "No allowlisted colour literal was seen; the palette or preview-data glob is broken.");

        if (failures.Count > 0)
        {
            var report = new StringBuilder();
            report.AppendLine($"{failures.Count} XAML colour literal(s) outside the palette allowlist:");
            foreach (var failure in failures.OrderBy(failure => failure, StringComparer.Ordinal))
            {
                report.AppendLine(failure);
            }

            Assert.Fail(report.ToString());
        }
    }

    private static bool IsAllowlisted(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        if (string.Equals(normalized, "Themes/ThemePickerData.xaml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Regex.IsMatch(normalized, "^Themes/[^/]+Theme\\.xaml$", RegexOptions.IgnoreCase);
    }

    private static bool IsFile(string path, string appRoot, params string[] relativeSegments)
    {
        var expected = Path.Combine(new[] { appRoot }.Concat(relativeSegments).ToArray());
        return string.Equals(path, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string RelativePath(string appRoot, string path)
    {
        return Path.GetRelativePath(appRoot, path);
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
