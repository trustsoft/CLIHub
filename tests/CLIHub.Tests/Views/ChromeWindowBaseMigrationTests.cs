namespace CLIHub.Tests.Views;

using System.Xml.Linq;

using static CLIHub.Tests.Support.TestHelpers;

/// <summary>
///   Guards the shared chromeless-window migration: the Settings, What's New, and Browse Plugins
///   windows are rooted in the <c>ChromeWindow</c> base, render <c>WindowHeaderBar</c> as their
///   drawn header, and carry none of the chrome attributes the base now owns.
/// </summary>
/// <remarks>
///   <para>
///     These are XAML text guards (the windows are never instantiated in tests), mirroring
///     <see cref="BrowsePluginsDialogBindingTests" />. Two adaptations from the original plan are
///     forced by verified code reality, both recorded as deviations in the change's design.md:
///   </para>
///   <para>
///     (1) The planned <c>x:ClassModifier="NotPublic"</c> assertion is impossible: the .NET WPF
///     markup compiler rejects any <c>x:ClassModifier</c> value with MC6013, so the guard asserts
///     the base-class derivation instead. (2) The base-owned background/font markers are checked on
///     the root element only: Browse Plugins' content area legitimately paints plugin cards and the
///     footer with <c>ChromeBgBrush</c>, and that content styling is deliberately left untouched by
///     the migration.
///   </para>
/// </remarks>
public sealed class ChromeWindowBaseMigrationTests
{
    private static readonly string[] ChromeWindowNames =
    {
        "SettingsWindow",
        "WhatsNewWindow",
        "BrowsePluginsDialog"
    };

    private static readonly string[] BaseOwnedRootAttributes =
    {
        "Background",
        "FontFamily"
    };

    private static readonly string[] BaseOwnedContentMarkers =
    {
        "WindowStyle=\"None\"",
        "KeyDown=\"OnKeyDown\"",
        "SourceInitialized="
    };

    [Fact]
    public void Windows_AreRootedInChromeWindow_WithBaseDerivation()
    {
        var root = FindRepositoryRoot();

        foreach (var name in ChromeWindowNames)
        {
            var xamlPath = Path.Combine(root, "src", "CLIHub", "Views", $"{name}.xaml");
            Assert.True(File.Exists(xamlPath), $"Expected {name}.xaml at '{xamlPath}'.");

            var content = File.ReadAllText(xamlPath);
            Assert.Contains($"views:ChromeWindow x:Class=\"CLIHub.Views.{name}\"", content);

            var codeBehindPath = Path.Combine(root, "src", "CLIHub", "Views", $"{name}.xaml.cs");
            Assert.True(File.Exists(codeBehindPath), $"Expected {name}.xaml.cs at '{codeBehindPath}'.");

            var codeBehind = File.ReadAllText(codeBehindPath);
            Assert.Contains($"partial class {name} : ChromeWindow", codeBehind);
        }
    }

    [Fact]
    public void Windows_DeclareWindowHeaderBar_WithExpectedTitle()
    {
        var root = FindRepositoryRoot();
        var expectedTitles = new Dictionary<string, string>
        {
            ["SettingsWindow"] = "Title=\"CLIHub - Settings\"",
            ["WhatsNewWindow"] = "Title=\"What's New\"",
            ["BrowsePluginsDialog"] = "Title=\"Browse Plugins\""
        };

        foreach (var (name, titleAttribute) in expectedTitles)
        {
            var content = File.ReadAllText(Path.Combine(root, "src", "CLIHub", "Views", $"{name}.xaml"));
            Assert.Contains("views:WindowHeaderBar", content);
            Assert.Contains(titleAttribute, content);
        }
    }

    [Fact]
    public void Windows_DoNotRedeclareBaseOwnedChrome()
    {
        var root = FindRepositoryRoot();

        foreach (var name in ChromeWindowNames)
        {
            var xamlPath = Path.Combine(root, "src", "CLIHub", "Views", $"{name}.xaml");
            var document = XDocument.Load(xamlPath, LoadOptions.None);
            var rootElement = document.Root;
            Assert.NotNull(rootElement);
            Assert.Equal("ChromeWindow", rootElement.Name.LocalName);

            // The chromeless profile lives on the base: the root redeclares no window-level
            // Background/FontFamily, and the document carries no OS-chrome or event wiring.
            foreach (var attribute in BaseOwnedRootAttributes)
            {
                Assert.Null(rootElement.Attribute(attribute));
            }

            foreach (var marker in BaseOwnedContentMarkers)
            {
                Assert.DoesNotContain(marker, File.ReadAllText(xamlPath), StringComparison.Ordinal);
            }

            Assert.DoesNotContain(
                document.Descendants(),
                element => element.Name.LocalName is "WindowChrome" or "WindowChrome.WindowChrome");
        }
    }

    [Fact]
    public void WindowHeaderBar_UsesSharedChromeStyles()
    {
        var root = FindRepositoryRoot();
        var headerBarPath = Path.Combine(root, "src", "CLIHub", "Views", "WindowHeaderBar.xaml");
        Assert.True(File.Exists(headerBarPath), $"Expected WindowHeaderBar.xaml at '{headerBarPath}'.");

        var content = File.ReadAllText(headerBarPath);

        // The header bar builds on the shared application-level keyed styles and glyph.
        Assert.Contains("Style=\"{StaticResource WindowHeader}\"", content);
        Assert.Contains("Style TargetType=\"TextBlock\" BasedOn=\"{StaticResource WindowTitle}\"", content);
        Assert.Contains("Style=\"{StaticResource WindowCloseButton}\"", content);
        Assert.Contains("themes:IconGlyphs.Close", content);
    }

    [Fact]
    public void WindowHeaderBar_CodeBehind_CallsInitializeComponent()
    {
        // The markup compiler generates InitializeComponent in the .g.cs partial only; without a
        // constructor call the BAML never loads, the control renders empty at zero size, and the
        // build and XAML text guards stay green — as caught by the manual smoke once.
        var root = FindRepositoryRoot();
        var codeBehindPath = Path.Combine(root, "src", "CLIHub", "Views", "WindowHeaderBar.xaml.cs");
        Assert.True(File.Exists(codeBehindPath), $"Expected WindowHeaderBar.xaml.cs at '{codeBehindPath}'.");

        var content = File.ReadAllText(codeBehindPath);
        Assert.Contains("this.InitializeComponent();", content);
    }
}
