namespace CLIHub.Tests.Themes;

using System.Text.RegularExpressions;

using static CLIHub.Tests.Support.TestHelpers;

public sealed class ThemePaletteParityTests
{
    [Fact]
    public void ThemePalettes_AllPaletteFilesDefineOneIdenticalKeySet()
    {
        var themesDirectory = Path.Combine(FindRepositoryRoot(), "src", "CLIHub", "Themes");
        var paletteFiles = Directory.GetFiles(themesDirectory, "*Theme.xaml")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();

        // Loud failure on rename or glob accidents: the check is only meaningful when it walks the
        // known palettes, so their presence is asserted explicitly.
        Assert.Contains("DarkTheme.xaml", paletteFiles.Select(Path.GetFileName));
        Assert.Contains("LightTheme.xaml", paletteFiles.Select(Path.GetFileName));

        var referenceKeys = ReadKeys(paletteFiles[0]);
        foreach (var file in paletteFiles.Skip(1))
        {
            var keys = ReadKeys(file);
            var missingInPalette = referenceKeys.Except(keys).OrderBy(key => key).ToArray();
            var missingInReference = keys.Except(referenceKeys).OrderBy(key => key).ToArray();

            Assert.True(
                missingInPalette.Length == 0 && missingInReference.Length == 0,
                $"Palette keys differ in {Path.GetFileName(file)}. " +
                $"Missing: [{string.Join(", ", missingInPalette)}]. " +
                $"Extra: [{string.Join(", ", missingInReference)}].");
        }

        // The accent is application-scoped and supplied at runtime, so a palette that declared an
        // accent key again would make the accent theme-bound.
        Assert.DoesNotContain("AccentBrush", referenceKeys);
        Assert.DoesNotContain("AccentSelectionBrush", referenceKeys);
    }

    private static HashSet<string> ReadKeys(string path)
    {
        var text = File.ReadAllText(path);
        return Regex.Matches(text, "x:Key=\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
    }
}
