namespace CLIHub.Core.Tests.Models;

using CLIHub.Core.Models;

public class AccentPaletteTests
{
    [Theory]
    [InlineData("#4cc2ff", "#4CC2FF")]
    [InlineData("4CC2FF", "#4CC2FF")]
    [InlineData("#FF4CC2FF", "#4CC2FF")]
    [InlineData("  #10b981  ", "#10B981")]
    public void TryNormalize_ValidValues_NormalizesToRrggbb(string input, string expected)
    {
        Assert.True(AccentPalette.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    [InlineData("not-a-color")]
    public void TryNormalize_InvalidValues_ReturnsFalse(string? input)
    {
        Assert.False(AccentPalette.TryNormalize(input, out _));
    }

    [Fact]
    public void Normalize_InvalidValue_FallsBackToDefault()
    {
        Assert.Equal(AccentPalette.DefaultHex, AccentPalette.Normalize("nonsense"));
    }

    [Fact]
    public void DefaultPreset_IsSky()
    {
        Assert.Equal(AccentPalette.DefaultHex, AccentPalette.Presets[0].Hex);
    }

    [Fact]
    public void Presets_AreNormalizedAndUnique()
    {
        Assert.All(
            AccentPalette.Presets,
            preset => Assert.True(
                AccentPalette.TryNormalize(preset.Hex, out var normalized) &&
                string.Equals(normalized, preset.Hex, StringComparison.Ordinal)));

        Assert.Equal(
            AccentPalette.Presets.Count,
            AccentPalette.Presets.Select(preset => preset.Name).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void SelectionTint_UsesLowAlpha()
    {
        Assert.Equal("#1F4CC2FF", AccentPalette.SelectionTint("#4CC2FF"));
    }

    [Fact]
    public void Opaque_PrependsFullAlpha()
    {
        Assert.Equal("#FF4CC2FF", AccentPalette.Opaque("#4CC2FF"));
    }
}
