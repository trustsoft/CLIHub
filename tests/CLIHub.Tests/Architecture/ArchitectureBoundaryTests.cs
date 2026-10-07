namespace CLIHub.Tests.Architecture;

public sealed class ArchitectureBoundaryTests
{
    private static readonly string[] WpfAllowedApplicationFiles =
    [
        "App.xaml.cs",
        "AssemblyInfo.cs",
        "ProjectDialogService.cs",
        "TrayIconController.cs",
        "UpdateDownloadNotifier.cs",
        "UserNotificationService.cs",
        "WpfApplicationLifetime.cs",
        "WpfApplicationStartupUi.cs"
    ];

    [Fact]
    public void CoreProject_DoesNotReferenceWpf()
    {
        var root = FindRepositoryRoot();
        var coreProject = File.ReadAllText(Path.Combine(root, "src", "CLIHub.Core", "CLIHub.Core.csproj"));
        var coreSource = EnumerateFiles(Path.Combine(root, "src", "CLIHub.Core"));

        Assert.DoesNotContain("UseWPF", coreProject, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Windows", coreProject, StringComparison.Ordinal);
        Assert.DoesNotContain(
            coreSource.Select(File.ReadAllText),
            content => content.Contains("System.Windows", StringComparison.Ordinal));
    }

    [Fact]
    public void ApplicationWpfReferences_AreLimitedToExplicitUiAdapters()
    {
        var root = FindRepositoryRoot();
        var sourceRoot = Path.Combine(root, "src", "CLIHub");
        var unexpected = EnumerateFiles(sourceRoot)
            .Where(path => !IsWpfAllowed(path, sourceRoot))
            .Where(path => File.ReadAllText(path).Contains("System.Windows", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        Assert.Empty(unexpected);
    }

    [Fact]
    public void DependencyRegistration_IsRestrictedToCompositionRoots()
    {
        var root = FindRepositoryRoot();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine("src", "CLIHub", "ServiceRegistration.cs"),
            Path.Combine("src", "CLIHub.Core", "Composition", "ServiceCollectionExtensions.cs")
        };
        var unexpected = EnumerateFiles(Path.Combine(root, "src"))
            .Where(path => File.ReadAllText(path).Contains("AddSingleton", StringComparison.Ordinal)
                || File.ReadAllText(path).Contains("AddTransient", StringComparison.Ordinal)
                || File.ReadAllText(path).Contains("AddScoped", StringComparison.Ordinal))
            .Where(path => !allowed.Contains(Path.GetRelativePath(root, path)))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        Assert.Empty(unexpected);
    }

    [Fact]
    public void ProductionSource_DoesNotReintroduceRetiredCompatibilityApis()
    {
        var root = FindRepositoryRoot();
        var retired = new[] { "IPluginManager", "PluginManager", "IProcessLauncher" };
        var matches = EnumerateFiles(Path.Combine(root, "src"))
            .SelectMany(path => retired
                .Where(symbol => File.ReadAllText(path).Contains(symbol, StringComparison.Ordinal))
                .Select(symbol => $"{Path.GetRelativePath(root, path)}: {symbol}"))
            .ToArray();

        Assert.Empty(matches);
    }

    [Fact]
    public void ProjectReferences_KeepApplicationAboveCore()
    {
        var root = FindRepositoryRoot();
        var coreProject = File.ReadAllText(Path.Combine(root, "src", "CLIHub.Core", "CLIHub.Core.csproj"));
        var applicationProject = File.ReadAllText(Path.Combine(root, "src", "CLIHub", "CLIHub.csproj"));

        Assert.DoesNotContain("CLIHub.csproj", coreProject, StringComparison.Ordinal);
        Assert.Contains("..\\CLIHub.Core\\CLIHub.Core.csproj", applicationProject, StringComparison.Ordinal);
    }

    [Fact]
    public void CiWorkflow_RunsBothTestProjects()
    {
        var workflow = File.ReadAllText(Path.Combine(FindRepositoryRoot(), ".github", "workflows", "ci.yml"));

        Assert.Contains("CLIHub.Core.Tests.csproj", workflow, StringComparison.Ordinal);
        Assert.Contains("CLIHub.Tests.csproj", workflow, StringComparison.Ordinal);
    }

    private static bool IsWpfAllowed(string path, string sourceRoot)
    {
        var relative = Path.GetRelativePath(sourceRoot, path);
        return relative.StartsWith("Views" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith("Converters" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith("Interop" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith("Hotkeys" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || WpfAllowedApplicationFiles.Contains(relative, StringComparer.OrdinalIgnoreCase)
            || string.Equals(relative, "TrayMenuBuilder.cs", StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith("ViewModels" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateFiles(string root) =>
        Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "artifacts" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

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
