namespace CLIHub.Tests.Themes;

using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

using static CLIHub.Tests.Support.TestHelpers;

/// <summary>
///   Loads <c> Themes/Common/SystemMenuStyles.xaml </c> from the repository file and guards its
///   shape: the implicit <see cref="ContextMenu" /> and <see cref="MenuItem" /> styles exist by
///   type key, the keyed <see cref="MenuItem.SeparatorStyleKey" /> style exists, and the two
///   system-menu palettes declare an identical key set that covers every <c> SystemMenu.* </c>
///   brush the style dictionary references.
/// </summary>
/// <remarks>
///   <para>
///     The style dictionary is parsed with <see cref="XamlReader" /> on a dedicated STA thread. Its
///     <c> /Themes/Common/Sizing.xaml </c> merge is redirected in memory to the same file on disk
///     through a <c> file:// </c> URI: <c> pack://application </c> URIs resolve only when a live
///     <c> Application </c> exists, and the test process deliberately creates none (see
///     <c> PaletteSwitcherTests </c>). The redirected attribute is the merge pointer only — the
///     styles, triggers, and brush references under test are the file's own bytes.
///   </para>
///   <para>
///     The two palette dictionaries declare no merges and no StaticResource references, so they
///     parse standalone without a redirect.
///   </para>
/// </remarks>
public sealed class SystemMenuStyleTests
{
    private static readonly Regex SystemMenuReferencePattern = new(
        "\\{DynamicResource\\s+(?<key>SystemMenu\\.[^\\s},]+)\\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void StylesDictionary_Load_ExposesImplicitContextMenuAndMenuItemStyles()
    {
        RunOnSta(() =>
        {
            var styles = LoadStylesDictionary();

            Assert.True(
                styles.Keys.Cast<object>().Contains(typeof(ContextMenu)),
                "The implicit ContextMenu style is missing.");
            Assert.True(
                styles.Keys.Cast<object>().Contains(typeof(MenuItem)),
                "The implicit MenuItem style is missing.");
            Assert.IsType<Style>(styles[typeof(ContextMenu)]);
            Assert.IsType<Style>(styles[typeof(MenuItem)]);
        });
    }

    [Fact]
    public void StylesDictionary_Load_ExposesTheKeyedSeparatorStyle()
    {
        RunOnSta(() =>
        {
            var styles = LoadStylesDictionary();

            Assert.True(
                styles.Keys.Cast<object>().Contains(MenuItem.SeparatorStyleKey),
                "The MenuItem.SeparatorStyleKey separator style is missing.");
            var separatorStyle = Assert.IsType<Style>(styles[MenuItem.SeparatorStyleKey]);
            Assert.Equal(typeof(Separator), separatorStyle.TargetType);
        });
    }

    [Fact]
    public void SystemMenuPalettes_DeclaredKeys_AreIdenticalAndCoverEveryReferencedBrush()
    {
        var appRoot = Path.Combine(FindRepositoryRoot(), "src", "CLIHub");
        var stylesText = File.ReadAllText(Path.Combine(appRoot, "Themes", "Common", "SystemMenuStyles.xaml"));
        var referencedKeys = SystemMenuReferencePattern.Matches(stylesText)
            .Select(match => match.Groups["key"].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        // Loud failure on a broken scan: the style dictionary must reference the menu palette.
        Assert.True(
            referencedKeys.Length > 0,
            "No SystemMenu.* DynamicResource reference was found in SystemMenuStyles.xaml.");

        var paletteKeySets = Directory.GetFiles(Path.Combine(appRoot, "Themes", "SystemMenu"), "*.xaml")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .Select(path => (Path: path, Keys: ReadDeclaredKeys(File.ReadAllText(path))))
            .ToArray();

        Assert.True(
            paletteKeySets.Length >= 2,
            $"Expected the dark and light system-menu palettes under '{appRoot}', but found {paletteKeySets.Length}.");

        var first = paletteKeySets[0];
        foreach (var other in paletteKeySets.Skip(1))
        {
            Assert.True(
                first.Keys.SetEquals(other.Keys),
                $"The system-menu palettes diverge: '{first.Path}' declares " +
                $"[{string.Join(", ", first.Keys.OrderBy(key => key, StringComparer.Ordinal))}] but " +
                $"'{other.Path}' declares [{string.Join(", ", other.Keys.OrderBy(key => key, StringComparer.Ordinal))}].");
        }

        var missing = referencedKeys.Where(key => !first.Keys.Contains(key)).ToArray();
        Assert.True(
            missing.Length == 0,
            $"The system-menu palettes do not declare the referenced brush(es): {string.Join(", ", missing)}.");
    }

    /// <summary>
    ///   Parses the repository <c> SystemMenuStyles.xaml </c> with its Sizing merge redirected to
    ///   the same file on disk; see the class remarks for the reason.
    /// </summary>
    private static ResourceDictionary LoadStylesDictionary()
    {
        var appRoot = Path.Combine(FindRepositoryRoot(), "src", "CLIHub");
        var stylesPath = Path.Combine(appRoot, "Themes", "Common", "SystemMenuStyles.xaml");
        var sizingPath = Path.Combine(appRoot, "Themes", "Common", "Sizing.xaml");
        var sizingUri = new Uri(sizingPath, UriKind.Absolute).ToString();

        var text = File.ReadAllText(stylesPath);
        Assert.Contains("Source=\"/Themes/Common/Sizing.xaml\"", text, StringComparison.Ordinal);
        var parseableText = text.Replace(
            "Source=\"/Themes/Common/Sizing.xaml\"",
            $"Source=\"{sizingUri}\"",
            StringComparison.Ordinal);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(parseableText));
        return (ResourceDictionary)XamlReader.Load(stream);
    }

    private static HashSet<string> ReadDeclaredKeys(string text)
    {
        return Regex.Matches(text, "x:Key=\"(?<key>[^\"]+)\"", RegexOptions.CultureInvariant)
            .Select(match => match.Groups["key"].Value)
            .ToHashSet(StringComparer.Ordinal);
    }
}