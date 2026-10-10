namespace CLIHub.Tests.Themes;

using System.Text.RegularExpressions;

public sealed class ThemePaletteParityTests
{
    [Fact]
    public void LightPalette_DefinesTheSameKeysAsDarkPalette()
    {
        var root = FindRepositoryRoot();
        var dark = ReadKeys(Path.Combine(root, "src", "CLIHub", "Themes", "LaunchTheme.xaml"));
        var light = ReadKeys(Path.Combine(root, "src", "CLIHub", "Themes", "LightTheme.xaml"));

        var missingInLight = dark.Except(light).OrderBy(key => key).ToArray();
        var missingInDark = light.Except(dark).OrderBy(key => key).ToArray();

        Assert.True(
            missingInLight.Length == 0 && missingInDark.Length == 0,
            $"Palette keys differ. Missing in light: [{string.Join(", ", missingInLight)}]. Missing in dark: [{string.Join(", ", missingInDark)}].");
    }

    private static HashSet<string> ReadKeys(string path)
    {
        var text = File.ReadAllText(path);
        return Regex.Matches(text, "x:Key=\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
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
