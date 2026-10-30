namespace CLIHub.Tests.Views;

using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using CLIHub.ViewModels;

using static CLIHub.Tests.Support.TestHelpers;

/// <summary>
///   Guards the string-key wiring of the Settings page host: the page metadata records template keys
///   and page keys as strings, so a rename on either side yields an empty header slot or a page that
///   never shows instead of a build error.
/// </summary>
public sealed class SettingsPageMetadataTests
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    // The page host expresses the page key inside the Visibility binding's markup extension:
    // {Binding SelectedPage.Key, Converter={StaticResource PageKeyVisibility}, ConverterParameter=<key>}.
    private static readonly Regex PageKeyParameterPattern = new(
        "ConverterParameter\\s*=\\s*(?<key>[^\\s,}]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void SettingsPages_TemplateKeysAndHostConverterParameters_AgreeWithSettingsWindow()
    {
        var root = FindRepositoryRoot();
        var settingsWindowPath = Path.Combine(root, "src", "CLIHub", "Views", "SettingsWindow.xaml");
        Assert.True(File.Exists(settingsWindowPath), $"Expected the Settings window XAML at '{settingsWindowPath}'.");

        var document = LoadDocument(settingsWindowPath);

        var templateKeys = document.Descendants()
            .Where(element => element.Name.LocalName == "DataTemplate")
            .Select(element => (string?)element.Attribute(XamlNamespace + "Key"))
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key!)
            .ToHashSet(StringComparer.Ordinal);

        var hostKeys = document.Descendants()
            .SelectMany(element => element.Attributes())
            .Where(attribute => attribute.Value.Contains("PageKeyVisibility", StringComparison.Ordinal))
            .SelectMany(attribute => PageKeyParameterPattern.Matches(attribute.Value)
                .Select(match => new
                {
                    Key = match.Groups["key"].Value,
                    Location = DescribeLocation(settingsWindowPath, attribute)
                }))
            .ToArray();

        var pages = ReadAllPages();

        // Loud failure on a regex/reflection accident: an empty side would make the comparison a no-op.
        Assert.Equal(9, pages.Length);
        Assert.True(
            templateKeys.Count >= 3,
            $"Expected at least three DataTemplate keys in SettingsWindow.xaml, found {templateKeys.Count}.");
        Assert.NotEmpty(hostKeys);

        var failures = new List<string>();

        var duplicatePageKeys = pages
            .GroupBy(page => page.Key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        foreach (var duplicateKey in duplicatePageKeys)
        {
            failures.Add($"AllPages declares the page key '{duplicateKey}' more than once.");
        }

        var pageKeys = pages.Select(page => page.Key).ToHashSet(StringComparer.Ordinal);
        var hostCounts = hostKeys
            .GroupBy(hostKey => hostKey.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        foreach (var page in pages.OrderBy(page => page.Key, StringComparer.Ordinal))
        {
            var slots = new[]
            {
                (Name: nameof(SettingsPage.HeaderTitleExtraKey), Key: page.HeaderTitleExtraKey),
                (Name: nameof(SettingsPage.HeaderActionKey), Key: page.HeaderActionKey),
                (Name: nameof(SettingsPage.SummaryRowKey), Key: page.SummaryRowKey)
            };

            foreach (var slot in slots.Where(slot => slot.Key is not null))
            {
                if (!templateKeys.Contains(slot.Key!))
                {
                    failures.Add(
                        $"Page '{page.Key}' names {slot.Name} '{slot.Key}', but SettingsWindow.xaml " +
                        $"declares only these DataTemplate keys: {FormatKeys(templateKeys)}.");
                }
            }

            if (!hostCounts.TryGetValue(page.Key, out var hostCount))
            {
                failures.Add(
                    $"Page '{page.Key}' from SettingsViewModel.AllPages has no PageKeyVisibility " +
                    "ConverterParameter in SettingsWindow.xaml.");
            }
            else if (hostCount != 1)
            {
                var locations = hostKeys
                    .Where(hostKey => string.Equals(hostKey.Key, page.Key, StringComparison.Ordinal))
                    .Select(hostKey => hostKey.Location);
                failures.Add(
                    $"Page '{page.Key}' appears {hostCount} times as a PageKeyVisibility ConverterParameter " +
                    $"in SettingsWindow.xaml ({string.Join(", ", locations)}); expected exactly once.");
            }
        }

        foreach (var hostKey in hostKeys
                     .Where(hostKey => !pageKeys.Contains(hostKey.Key))
                     .OrderBy(hostKey => hostKey.Location, StringComparer.Ordinal))
        {
            failures.Add(
                $"ConverterParameter '{hostKey.Key}' at {hostKey.Location} matches no SettingsViewModel.AllPages key. " +
                $"Page keys: {FormatKeys(pageKeys)}.");
        }

        if (failures.Count > 0)
        {
            var report = new StringBuilder();
            report.AppendLine($"{failures.Count} Settings page metadata problem(s):");
            foreach (var failure in failures.OrderBy(failure => failure, StringComparer.Ordinal))
            {
                report.AppendLine(failure);
            }

            Assert.Fail(report.ToString());
        }
    }

    private static SettingsPage[] ReadAllPages() => SettingsPageCatalog.AllPages.ToArray();

    private static XDocument LoadDocument(string path)
    {
        using var stream = File.OpenRead(path);
        return XDocument.Load(stream, LoadOptions.SetLineInfo);
    }

    private static string DescribeLocation(string path, XObject node)
    {
        if (node is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            return $"{path}:{lineInfo.LineNumber}";
        }

        return path;
    }

    private static string FormatKeys(IEnumerable<string> keys)
    {
        var ordered = keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();
        return "[" + string.Join(", ", ordered) + "]";
    }
}
