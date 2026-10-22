namespace CLIHub.Tests.Themes;

using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

/// <summary>
///   Statically verifies that every <c>StaticResource</c> and <c>DynamicResource</c> reference in
///   the application's XAML resolves against the keys reachable from the file that contains it.
/// </summary>
/// <remarks>
///   <para>
///     The model mirrors the WPF load boundaries that matter for this repository:
///   </para>
///   <list type="bullet">
///     <item>
///       A loose <c>ResourceDictionary</c> file resolves <c>StaticResource</c> only against its own
///       keys plus its merged dictionaries. It does not reach application-level siblings.
///     </item>
///     <item>
///       A loose dictionary's <c>DynamicResource</c> references are additionally allowed to resolve
///       from application-level resources, because they are evaluated at runtime through the
///       element tree.
///     </item>
///     <item>
///       <c>Window</c>, <c>UserControl</c> and <c>Application</c> files can reach their own keys,
///       their merged dictionaries, and the application-level key set. That application-level set is
///       App.xaml's local keys plus App.xaml's transitive merges only: window-scoped keys are
///       deliberately absent, so a Settings page (a UserControl built before it attaches to the
///       window tree) cannot accidentally depend on <c>SettingsWindow.xaml</c> resources.
///     </item>
///   </list>
///   <para>
///     Out of scope, because they are either compile-time checked or not resource lookups:
///     <c>x:Static</c> references, <c>TemplateBinding</c>, and <c>Binding</c> property paths.
///   </para>
///   <para>
///     Keys written into <c>Application.Resources</c> by code (at startup and on settings save) are
///     listed in <see cref="RuntimeProvidedKeys"/>; every other referenced key must be declared in XAML.
///   </para>
///   <para>
///     Scope approximation: every <c>x:Key</c> in a file is treated as visible to every reference in
///     that file, even when it is declared inside a nested template. This can miss a template-local
///     scope error but never reports a valid reference as missing.
///   </para>
/// </remarks>
public sealed class XamlResourceReferenceTests
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    // Resource keys written into Application.Resources by code instead of being declared in XAML:
    // AccentApplier assigns the accent brushes at startup and again on a Settings save, and
    // ApplicationHost registers the path-to-image converter from the DI container. Keep this list
    // deliberately narrow; every other key must be declared in XAML somewhere reachable from its
    // reference.
    private static readonly HashSet<string> RuntimeProvidedKeys = new(StringComparer.Ordinal)
    {
        // AccentApplier — applied from App.OnStartup and re-applied by WpfAccentPreferenceTarget.
        "AccentBrush",
        "AccentSelectionBrush",

        // PathToImageConverter.ResourceKey — registered from the DI container in ApplicationHost.
        "PathToImage"
    };

    private static readonly Regex ResourceReferencePattern = new(
        @"\{(?<kind>Static|Dynamic)Resource\s+(?<key>[^{}]+)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex UnsupportedResourceReferencePattern = new(
        @"\{(?:Static|Dynamic)Resource\s+\{",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void XamlFiles_AllResourceReferencesResolve()
    {
        var appRoot = Path.Combine(FindRepositoryRoot(), "src", "CLIHub");
        var xamlFiles = Directory.GetFiles(appRoot, "*.xaml", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        // Loud failure on glob or rename accidents: the walk is only meaningful when it sees the
        // known application XAML tree.
        Assert.True(
            xamlFiles.Length >= 20,
            $"Expected the full XAML tree under '{appRoot}', but found only {xamlFiles.Length} files.");
        Assert.Contains(xamlFiles, path => path == Path.Combine(appRoot, "App.xaml"));
        Assert.Contains(xamlFiles, path => path.EndsWith(Path.Combine("Themes", "SettingsStyles.xaml"), StringComparison.Ordinal));
        Assert.Contains(xamlFiles, path => path.EndsWith(Path.Combine("Views", "SettingsWindow.xaml"), StringComparison.Ordinal));

        var documents = xamlFiles.ToDictionary(path => path, LoadDocument, StringComparer.OrdinalIgnoreCase);
        var mergedSourceFailures = new List<string>();
        var mergedKeysByFile = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        HashSet<string> ReadMergedKeys(string path)
        {
            if (mergedKeysByFile.TryGetValue(path, out var cachedKeys))
            {
                return cachedKeys;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { path };
            var pending = new Stack<string>();
            foreach (var sourcePath in ReadMergedSourcePaths(documents[path], path, appRoot, mergedSourceFailures))
            {
                pending.Push(sourcePath);
            }

            while (pending.Count > 0)
            {
                var currentPath = pending.Pop();
                if (!visited.Add(currentPath))
                {
                    continue;
                }

                if (!documents.TryGetValue(currentPath, out var currentDocument))
                {
                    // Any missing file is reported by ReadMergedSourcePaths at its Source site.
                    continue;
                }

                keys.UnionWith(ReadLocalKeys(currentDocument));
                foreach (var sourcePath in ReadMergedSourcePaths(currentDocument, currentPath, appRoot, mergedSourceFailures))
                {
                    pending.Push(sourcePath);
                }
            }

            mergedKeysByFile[path] = keys;
            return keys;
        }

        foreach (var path in documents.Keys)
        {
            _ = ReadMergedKeys(path);
        }

        // Application-level set: App.xaml's own keys and its transitive merges. Palette dictionaries
        // merged at runtime define the same key set as the dark base (guarded by the palette parity
        // test), so the dark base is a sound representation of the application palette here.
        var appPath = Path.Combine(appRoot, "App.xaml");
        var applicationKeys = new HashSet<string>(ReadLocalKeys(documents[appPath]), StringComparer.Ordinal);
        applicationKeys.UnionWith(ReadMergedKeys(appPath));
        applicationKeys.UnionWith(RuntimeProvidedKeys);

        Assert.True(
            applicationKeys.Count > 0,
            "The application-level key set is empty; App.xaml merges were not modeled.");

        var failures = new List<string>(mergedSourceFailures);
        var referenceCount = 0;
        var staticReferenceCount = 0;
        var dynamicReferenceCount = 0;

        foreach (var (path, document) in documents)
        {
            var isResourceDictionary = document.Root?.Name.LocalName == "ResourceDictionary";
            var localKeys = ReadLocalKeys(document);
            var mergedKeys = ReadMergedKeys(path);

            foreach (var element in document.Descendants())
            {
                foreach (var attribute in element.Attributes())
                {
                    var value = attribute.Value;
                    if (!value.Contains("Resource", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (UnsupportedResourceReferencePattern.IsMatch(value))
                    {
                        failures.Add(
                            $"{DescribeLocation(path, attribute)}: nested markup extension resource keys " +
                            "are not modeled by this test; extend the test before using this form.");
                        continue;
                    }

                    foreach (Match match in ResourceReferencePattern.Matches(value))
                    {
                        referenceCount++;
                        var kind = match.Groups["kind"].Value;
                        if (kind == "Static")
                        {
                            staticReferenceCount++;
                        }
                        else
                        {
                            dynamicReferenceCount++;
                        }

                        var key = match.Groups["key"].Value.Trim();
                        var assignedValueIndex = key.IndexOf('=', StringComparison.Ordinal);
                        if (assignedValueIndex >= 0)
                        {
                            key = key[(assignedValueIndex + 1)..].Trim();
                        }

                        if (key.Length == 0 || key.Any(char.IsWhiteSpace))
                        {
                            failures.Add(
                                $"{DescribeLocation(path, attribute)}: unsupported {kind}Resource reference " +
                                $"form '{match.Value}'.");
                            continue;
                        }

                        // StaticResource in a dictionary is resolved at load time against that
                        // dictionary and its merges only. DynamicResource (and anything inside a
                        // Window/UserControl/Application) can walk up to application resources.
                        var reachableKeys = new HashSet<string>(localKeys, StringComparer.Ordinal);
                        reachableKeys.UnionWith(mergedKeys);
                        if (!isResourceDictionary || kind == "Dynamic")
                        {
                            reachableKeys.UnionWith(applicationKeys);
                        }

                        if (!reachableKeys.Contains(key))
                        {
                            failures.Add(
                                $"{DescribeLocation(path, attribute)}: missing {kind}Resource key '{key}'. " +
                                "Reachable keys: " + FormatKeys(reachableKeys));
                        }
                    }
                }
            }
        }

        // Guard against a silent no-op walk: both reference kinds must actually be exercised.
        Assert.True(referenceCount > 0, "No StaticResource or DynamicResource references were found.");
        Assert.True(
            staticReferenceCount > 0 && dynamicReferenceCount > 0,
            $"Expected both reference kinds in the XAML tree, found {staticReferenceCount} static and " +
            $"{dynamicReferenceCount} dynamic references.");

        if (failures.Count > 0)
        {
            var report = new StringBuilder();
            report.AppendLine($"{failures.Count} XAML resource reference problem(s):");
            foreach (var failure in failures.OrderBy(failure => failure, StringComparer.Ordinal))
            {
                report.AppendLine(failure);
            }

            Assert.Fail(report.ToString());
        }
    }

    private static XDocument LoadDocument(string path)
    {
        using var stream = File.OpenRead(path);
        return XDocument.Load(stream, LoadOptions.SetLineInfo);
    }

    private static HashSet<string> ReadLocalKeys(XDocument document)
    {
        return document.Descendants()
            .Attributes(XamlNamespace + "Key")
            .Select(attribute => attribute.Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static List<string> ReadMergedSourcePaths(
        XDocument document,
        string filePath,
        string appRoot,
        ICollection<string> failures)
    {
        var resolvedPaths = new List<string>();
        foreach (var dictionary in document.Descendants().Where(element => element.Name.LocalName == "ResourceDictionary"))
        {
            var source = dictionary.Attribute("Source")?.Value;
            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            var resolvedPath = ResolveSourcePath(source, appRoot);
            if (resolvedPath is null)
            {
                failures.Add($"{filePath}: merged dictionary Source=\"{source}\" does not resolve inside {appRoot}.");
                continue;
            }

            resolvedPaths.Add(resolvedPath);
        }

        return resolvedPaths;
    }

    private static string? ResolveSourcePath(string source, string appRoot)
    {
        var normalizedRoot = Path.GetFullPath(appRoot);
        var relativePath = source.Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath));

        var rootPrefix = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
    }

    private static bool IsBuildOutput(string path)
    {
        var segments = path.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
        return segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
            || segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
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
