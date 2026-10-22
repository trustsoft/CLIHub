namespace CLIHub.Tests.Themes;

using System.Windows;

using CLIHub.Core.Models;
using CLIHub.Themes;

/// <summary>
///   Covers the tracked palette overlay of <see cref="PaletteSwitcher" />. Every test uses a fresh
///   instance (never <see cref="PaletteSwitcher.Shared" />) against a local collection, so the
///   process-wide overlay of the running application is never touched.
/// </summary>
/// <remarks>
///   The test process deliberately creates no <see cref="System.Windows.Application" /> (asserted by
///   <c> ResourceKeyToTemplateConverterTests </c>), and WPF resolves
///   <c> pack://application:,,,/CLIHub;component/... </c> resource dictionaries only once one
///   exists. The tests therefore use the internal palette-factory seam and assert the path resolved
///   through <see cref="ThemeCatalog" />; the slot behavior under test is unchanged.
/// </remarks>
public sealed class PaletteSwitcherTests
{
    [Fact]
    public void Apply_LightOnEmptyCollection_AddsOnePaletteDictionaryForTheResolvedPath()
    {
        var requested = new List<string>();
        var palette = new ResourceDictionary();
        var merged = new List<ResourceDictionary>();
        var switcher = new PaletteSwitcher(path =>
        {
            requested.Add(path);
            return palette;
        });

        var applied = switcher.Apply(merged, AppTheme.Light, systemPrefersLight: false);

        Assert.True(applied);
        Assert.Same(palette, Assert.Single(merged));
        var path = Assert.Single(requested);
        Assert.Equal(ThemeCatalog.ResolvePalettePath(AppTheme.Light, systemPrefersLight: false), path);
        Assert.Contains("LightTheme.xaml", path, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_BlueAfterLight_KeepsTheCountAndReplacesTheSameSlot()
    {
        var requested = new List<string>();
        var created = new List<ResourceDictionary>();
        var merged = new List<ResourceDictionary>();
        var switcher = new PaletteSwitcher(path =>
        {
            requested.Add(path);
            created.Add(new ResourceDictionary());
            return created[^1];
        });
        Assert.True(switcher.Apply(merged, AppTheme.Light, systemPrefersLight: false));

        var applied = switcher.Apply(merged, AppTheme.Blue, systemPrefersLight: false);

        Assert.True(applied);
        Assert.Equal(2, requested.Count);
        Assert.Contains("BlueTheme.xaml", requested[1], StringComparison.Ordinal);
        var blue = Assert.Single(merged);
        Assert.Same(created[1], blue);
        Assert.NotSame(created[0], blue);
    }

    [Fact]
    public void Apply_DarkAfterLight_RemovesTheOverlay()
    {
        var requested = new List<string>();
        var merged = new List<ResourceDictionary>();
        var switcher = new PaletteSwitcher(path =>
        {
            requested.Add(path);
            return new ResourceDictionary();
        });
        Assert.True(switcher.Apply(merged, AppTheme.Light, systemPrefersLight: false));

        var applied = switcher.Apply(merged, AppTheme.Dark, systemPrefersLight: false);

        Assert.True(applied);
        Assert.Single(requested);
        Assert.Empty(merged);
    }

    [Fact]
    public void Apply_DarkTwice_IsANoOp()
    {
        var merged = new List<ResourceDictionary>();
        var switcher = new PaletteSwitcher(
            _ => throw new InvalidOperationException("The dark base palette needs no overlay."));

        Assert.True(switcher.Apply(merged, AppTheme.Dark, systemPrefersLight: false));
        Assert.True(switcher.Apply(merged, AppTheme.Dark, systemPrefersLight: false));

        Assert.Empty(merged);
    }

    [Fact]
    public void Apply_PaletteThatCannotLoad_ReturnsFalseAndLeavesTheCollectionUnchanged()
    {
        var unrelated = new ResourceDictionary();
        var merged = new List<ResourceDictionary> { unrelated };
        var switcher = new PaletteSwitcher(_ => throw new IOException("The palette file is missing."));

        var applied = switcher.Apply(merged, AppTheme.Light, systemPrefersLight: false);

        Assert.False(applied);
        Assert.Same(unrelated, Assert.Single(merged));
    }
}
