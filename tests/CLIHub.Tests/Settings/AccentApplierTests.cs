namespace CLIHub.Tests.Settings;

using System.Threading;
using System.Windows;
using System.Windows.Media;

using CLIHub.Settings;

using static CLIHub.Tests.Support.TestHelpers;

/// <summary>
///   Covers <see cref="AccentApplier" />: the accent brushes are written into the supplied
///   application resources, independent of any merged palette.
/// </summary>
/// <remarks>
///   WPF brushes and resource dictionaries are created on an STA thread, following the pattern used
///   by the converter tests. The test process intentionally creates no WPF application, so the
///   applier is exercised through the resource dictionary it is given, exactly as production callers
///   pass <see cref="Application.Resources" />.
/// </remarks>
public sealed class AccentApplierTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-color")]
    [InlineData("#12345")]
    [InlineData("#4CC2FF")]
    public void Apply_DefaultMissingOrInvalidAccent_WritesSkyBrushes(string? accentHex) => RunOnSta(() =>
    {
        var resources = new ResourceDictionary();

        AccentApplier.Apply(resources, accentHex);

        AssertBrush(resources, "AccentBrush", "#FF4CC2FF");
        AssertBrush(resources, "AccentSelectionBrush", "#1F4CC2FF");
    }, "STA accent applier test failed.");

    [Fact]
    public void Apply_CustomAccent_WritesThatColorAndItsTint() => RunOnSta(() =>
    {
        var resources = new ResourceDictionary();

        AccentApplier.Apply(resources, "#0d9488");

        AssertBrush(resources, "AccentBrush", "#FF0D9488");
        AssertBrush(resources, "AccentSelectionBrush", "#1F0D9488");
    }, "STA accent applier test failed.");

    [Fact]
    public void Apply_CalledTwice_ReplacesBothEntries() => RunOnSta(() =>
    {
        var resources = new ResourceDictionary();

        AccentApplier.Apply(resources, "#4CC2FF");
        AccentApplier.Apply(resources, "#EA580C");

        AssertBrush(resources, "AccentBrush", "#FFEA580C");
        AssertBrush(resources, "AccentSelectionBrush", "#1FEA580C");
        Assert.Equal(
            2,
            resources.Keys.Cast<object>().Count(key => key is "AccentBrush" or "AccentSelectionBrush"));
    }, "STA accent applier test failed.");

    [Fact]
    public void Apply_EmptyResourceDictionary_DoesNotThrow() => RunOnSta(() =>
    {
        var resources = new ResourceDictionary();
        Assert.Empty(resources);

        var exception = Record.Exception(() => AccentApplier.Apply(resources, "#4CC2FF"));

        Assert.Null(exception);
        Assert.Equal(2, resources.Count);
    }, "STA accent applier test failed.");

    [Fact]
    public void Apply_ThenPaletteSwap_LeavesBothAccentEntriesUntouched() => RunOnSta(() =>
    {
        var resources = new ResourceDictionary();
        AccentApplier.Apply(resources, "#10B981");
        var accent = resources["AccentBrush"];
        var tint = resources["AccentSelectionBrush"];

        var palette = new ResourceDictionary { ["ChromeBgBrush"] = CreateBrush("#FF26262C") };
        resources.MergedDictionaries.Add(palette);
        Assert.Same(accent, resources["AccentBrush"]);
        Assert.Same(tint, resources["AccentSelectionBrush"]);

        var replacement = new ResourceDictionary { ["ChromeBgBrush"] = CreateBrush("#FF232833") };
        resources.MergedDictionaries[0] = replacement;
        Assert.Same(accent, resources["AccentBrush"]);
        Assert.Same(tint, resources["AccentSelectionBrush"]);

        resources.MergedDictionaries.Remove(replacement);
        Assert.Empty(resources.MergedDictionaries);
        Assert.Same(accent, resources["AccentBrush"]);
        Assert.Same(tint, resources["AccentSelectionBrush"]);
    }, "STA accent applier test failed.");

    [Fact]
    public void Apply_WithBrushFactory_ReceivesOpaqueAndTintValues() => RunOnSta(() =>
    {
        var resources = new ResourceDictionary();
        var requested = new List<string>();

        AccentApplier.Apply(
            resources,
            "#3b82f6",
            argbHex =>
            {
                requested.Add(argbHex);
                return CreateBrush(argbHex);
            });

        Assert.Equal(new[] { "#FF3B82F6", "#1F3B82F6" }, requested);
        AssertBrush(resources, "AccentBrush", "#FF3B82F6");
        AssertBrush(resources, "AccentSelectionBrush", "#1F3B82F6");
    }, "STA accent applier test failed.");

    private static void AssertBrush(ResourceDictionary resources, string key, string expectedArgbHex)
    {
        var brush = Assert.IsType<SolidColorBrush>(resources[key]);
        Assert.Equal((Color)ColorConverter.ConvertFromString(expectedArgbHex), brush.Color);
    }

    private static SolidColorBrush CreateBrush(string argbHex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(argbHex));
        brush.Freeze();
        return brush;
    }
}
