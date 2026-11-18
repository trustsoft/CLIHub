namespace CLIHub.Tests.Themes;

using System.Windows;

using CLIHub.Theming;

/// <summary>
///   Covers the tracked menu-palette overlay of <see cref="SystemMenuTheme" />. Every test uses a
///   fresh instance (never <see cref="SystemMenuTheme.Shared" />) against a local collection, so
///   the process-wide overlay of the running application is never touched.
/// </summary>
/// <remarks>
///   The test process deliberately creates no <see cref="System.Windows.Application" /> (asserted by
///   <c> ResourceKeyToTemplateConverterTests </c>), and WPF resolves
///   <c> pack://application:,,,/CLIHub;component/... </c> resource dictionaries only once one
///   exists. The tests therefore use the internal palette-factory seam and assert the path resolved
///   for the requested Windows mode; the slot behavior under test is unchanged.
/// </remarks>
public sealed class SystemMenuThemeTests
{
    [Fact]
    public void Apply_LightOnEmptyCollection_AddsOneMenuPaletteDictionary()
    {
        var requested = new List<string>();
        var palette = new ResourceDictionary();
        var merged = new List<ResourceDictionary>();
        var theme = new SystemMenuTheme(path =>
        {
            requested.Add(path);
            return palette;
        });

        var applied = theme.Apply(merged, systemPrefersLight: true);

        Assert.True(applied);
        Assert.Same(palette, Assert.Single(merged));
        var path = Assert.Single(requested);
        Assert.Contains("SystemMenuLight.xaml", path, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_DarkAfterLight_KeepsTheCountAndReplacesTheSameSlot()
    {
        var requested = new List<string>();
        var created = new List<ResourceDictionary>();
        var merged = new List<ResourceDictionary>();
        var theme = new SystemMenuTheme(path =>
        {
            requested.Add(path);
            created.Add(new ResourceDictionary());
            return created[^1];
        });
        Assert.True(theme.Apply(merged, systemPrefersLight: true));

        var applied = theme.Apply(merged, systemPrefersLight: false);

        Assert.True(applied);
        Assert.Equal(2, requested.Count);
        Assert.Contains("SystemMenuDark.xaml", requested[1], StringComparison.Ordinal);
        var dark = Assert.Single(merged);
        Assert.Same(created[1], dark);
        Assert.NotSame(created[0], dark);
    }

    [Fact]
    public void Apply_CalledTwice_TracksExactlyOneOverlay()
    {
        var merged = new List<ResourceDictionary>();
        var theme = new SystemMenuTheme(_ => new ResourceDictionary());

        Assert.True(theme.Apply(merged, systemPrefersLight: false));
        Assert.True(theme.Apply(merged, systemPrefersLight: true));

        Assert.Single(merged);
    }

    [Fact]
    public void Apply_DictionaryThatCannotLoad_ReturnsFalseAndLeavesTheCollectionUnchanged()
    {
        var unrelated = new ResourceDictionary();
        var merged = new List<ResourceDictionary> { unrelated };
        var theme = new SystemMenuTheme(_ => throw new IOException("The menu palette file is missing."));

        var applied = theme.Apply(merged, systemPrefersLight: false);

        Assert.False(applied);
        Assert.Same(unrelated, Assert.Single(merged));
    }
}