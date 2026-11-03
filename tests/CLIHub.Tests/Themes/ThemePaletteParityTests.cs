namespace CLIHub.Tests.Themes;

using System.Text.RegularExpressions;

using static CLIHub.Tests.Support.TestHelpers;

public sealed class ThemePaletteParityTests
{
    [Fact]
    public void ThemePalettes_AllPaletteFilesDefineOneIdenticalKeySet()
    {
        var themesDirectory = Path.Combine(FindRepositoryRoot(), "src", "CLIHub", "Themes");
        var paletteFiles = Directory.GetFiles(themesDirectory, "*.xaml")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();

        // Loud failure on rename or glob accidents: the check is only meaningful when it walks the
        // known palettes, so their presence is asserted explicitly.
        Assert.Contains("Dark.xaml", paletteFiles.Select(Path.GetFileName));
        Assert.Contains("Light.xaml", paletteFiles.Select(Path.GetFileName));
        Assert.Contains("Blue.xaml", paletteFiles.Select(Path.GetFileName));

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

        // 1. Ordered key comparison
        var referenceKeyList = ReadKeysOrdered(paletteFiles[0]);
        foreach (var file in paletteFiles.Skip(1))
        {
            var keyList = ReadKeysOrdered(file);
            Assert.True(
                referenceKeyList.SequenceEqual(keyList),
                $"Key order differs in {Path.GetFileName(file)}. Expected: [{string.Join(", ", referenceKeyList)}]. Actual: [{string.Join(", ", keyList)}].");
        }

        // 2. Normalized shape comparison
        // xUnit's Assert.Equal has no overload with a custom message, so use Assert.True.
        var referenceNormalized = NormalizeShape(File.ReadAllText(paletteFiles[0]));
        Assert.True(referenceNormalized.Contains("x:Key"), "Normalization must preserve key declarations.");
        foreach (var file in paletteFiles.Skip(1))
        {
            var normalized = NormalizeShape(File.ReadAllText(file));
            Assert.True(normalized.Contains("x:Key"), "Normalization must preserve key declarations.");
            Assert.True(
                string.Equals(referenceNormalized, normalized, StringComparison.Ordinal),
                $"File shape differs in {Path.GetFileName(file)} after masking Color attributes. " +
                "The three palettes must have identical structure (comments, whitespace, key order) " +
                "differing only in Color values. Check the normalized diff.");
        }

        // The accent is application-scoped and supplied at runtime, so a palette that declared an
        // accent key again would make the accent theme-bound.
        Assert.DoesNotContain("AccentBrush", referenceKeys);
        Assert.DoesNotContain("AccentSelectionBrush", referenceKeys);
    }

    [Fact]
    public void NormalizeShape_MixedColorAndPlainHexText_MasksOnlyColorAttributes()
    {
        var xaml = string.Join(
            "\n",
            "<SolidColorBrush x:Key=\"SampleBrush\" Color=\"#FF26262C\"/>",
            "<SolidColorBrush x:Key=\"AlphaBrush\" Color=\"#1Affffff\"/>",
            "<SolidColorBrush x:Key=\"RgbBrush\" Color=\"#4ADE80\"/>",
            "<!-- #FF26262C inside a comment is not a Color attribute -->",
            "<Text Tag=\"#ABCDEF\"/>");

        var normalized = NormalizeShape(xaml);

        // #RRGGBB and #AARRGGBB Color attributes are masked, lowercase hex included.
        Assert.DoesNotContain("Color=\"#FF26262C\"", normalized);
        Assert.DoesNotContain("Color=\"#1Affffff\"", normalized);
        Assert.DoesNotContain("Color=\"#4ADE80\"", normalized);

        // Hex-looking text outside a Color="..." attribute stays verbatim, and so does every
        // non-Color line: the mask only rewrites the Color attribute values.
        Assert.Contains("#FF26262C inside a comment is not a Color attribute", normalized);
        Assert.Contains("<Text Tag=\"#ABCDEF\"/>", normalized);

        var expected = string.Join(
            Environment.NewLine,
            "<SolidColorBrush x:Key=\"SampleBrush\" Color=\"#FFFFFFFF\"/>",
            "<SolidColorBrush x:Key=\"AlphaBrush\" Color=\"#FFFFFFFF\"/>",
            "<SolidColorBrush x:Key=\"RgbBrush\" Color=\"#FFFFFFFF\"/>",
            "<!-- #FF26262C inside a comment is not a Color attribute -->",
            "<Text Tag=\"#ABCDEF\"/>");
        Assert.Equal(expected, normalized);
    }

    private static HashSet<string> ReadKeys(string path)
    {
        var text = File.ReadAllText(path);
        return Regex.Matches(text, "x:Key=\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static List<string> ReadKeysOrdered(string path)
    {
        var text = File.ReadAllText(path);
        return Regex.Matches(text, @"x:Key=""([^""]+)""")
            .Select(match => match.Groups[1].Value)
            .ToList();
    }

    private static string NormalizeShape(string xaml)
    {
        // Mask all Color="#..." attributes with a fixed dummy value
        var normalized = Regex.Replace(xaml, @"Color=""#[0-9A-Fa-f]{6,8}""", @"Color=""#FFFFFFFF""");

        // Split on both line-ending styles and rejoin with Environment.NewLine, so a CRLF/LF
        // mismatch cannot fail the shape comparison; trailing whitespace is trimmed per line.
        var lines = normalized.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        return string.Join(Environment.NewLine, lines.Select(line => line.TrimEnd()));
    }
}
