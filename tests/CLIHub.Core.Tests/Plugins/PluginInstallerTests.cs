namespace CLIHub.Core.Tests.Plugins;

using System.IO.Compression;
using System.Text;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Plugins;

public class PluginInstallerTests : IDisposable
{
    private readonly string root;
    private readonly string pluginsPath;
    private readonly string stateFilePath;
    private readonly Mock<IPluginCatalog> catalog = new();

    public PluginInstallerTests()
    {
        this.root = Path.Combine(Path.GetTempPath(), "clihub-installer-" + Guid.NewGuid().ToString("N"));
        this.pluginsPath = Path.Combine(this.root, "plugins");
        this.stateFilePath = Path.Combine(this.root, "plugin-state.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(this.root, recursive: true); } catch { }
    }

    private PluginInstaller CreateInstaller(IPluginDescriptorReader? reader = null) =>
        new(NullLogger<PluginInstaller>.Instance,
            new PluginStateRepository(NullLogger<PluginStateRepository>.Instance, this.stateFilePath),
            this.catalog.Object,
            reader,
            pluginsPath: this.pluginsPath);

    private static string CreatePackage(params (string EntryName, byte[] Content)[] entries)
    {
        var path = Path.Combine(Path.GetTempPath(), "clihub-package-" + Guid.NewGuid().ToString("N") + ".clihub-plugin.zip");
        using var stream = File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var (entryName, content) in entries)
        {
            var entry = archive.CreateEntry(entryName);
            using var destination = entry.Open();
            destination.Write(content);
        }

        return path;
    }

    private static byte[] Text(string content) => Encoding.UTF8.GetBytes(content);

    private static byte[] LogoBytes => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A];

    private const string ValidDescriptor = """
    {
      "id": "community",
      "name": "Community Agent",
      "version": "1.2.3",
      "publisher": "Community",
      "homepage": "https://example.com/community",
      "commands": {
        "launch": { "executable": "community" },
        "resume": { "executable": "community", "arguments": "--continue" },
        "version": { "executable": "community", "arguments": "--version" }
      }
    }
    """;

    private static string ValidPackage() =>
        CreatePackage(
            ("community/plugin.json", Text(ValidDescriptor)),
            ("community/logo.png", LogoBytes));

    private void WriteExistingPlugin(string id, string json, string? installedVersion = null)
    {
        var directory = Path.Combine(this.pluginsPath, id);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "plugin.json"), json);
        if (installedVersion is not null)
        {
            var repository = new PluginStateRepository(NullLogger<PluginStateRepository>.Instance, this.stateFilePath);
            var document = repository.Load();
            document.Plugins[id] = new PluginStateEntry(PluginOrigin.Installed) { InstalledVersion = installedVersion };
            repository.Save(document);
        }
    }

    private string[] PluginRootSubdirectories() =>
        Directory.Exists(this.pluginsPath)
            ? Directory.GetDirectories(this.pluginsPath).Select(Path.GetFileName).Where(n => n is not null).Select(n => n!).ToArray()
            : [];

    [Fact]
    public void Inspect_ValidPackage_ReturnsPreviewWithAllDeclaredCommands()
    {
        var preview = this.CreateInstaller().Inspect(ValidPackage());

        Assert.True(preview.IsValid);
        Assert.Empty(preview.Errors);
        Assert.Equal("community", preview.PluginId);
        Assert.Equal("Community Agent", preview.PluginName);
        Assert.Equal("1.2.3", preview.Version);
        Assert.Equal("Community", preview.Publisher);
        Assert.Equal("https://example.com/community", preview.Homepage);
        Assert.False(preview.ReplacesExistingPlugin);
        Assert.Null(preview.ExistingPluginVersion);

        Assert.Equal(3, preview.DeclaredCommands.Count);
        Assert.Equal(new CommandPreview("launch", "community", null), preview.DeclaredCommands[0]);
        Assert.Equal(new CommandPreview("resume", "community", "--continue"), preview.DeclaredCommands[1]);
        Assert.Equal(new CommandPreview("version", "community", "--version"), preview.DeclaredCommands[2]);
    }

    [Fact]
    public void Inspect_MissingPluginJson_Rejected()
    {
        var package = CreatePackage(("community/logo.png", LogoBytes));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("missing", StringComparison.Ordinal)
            && error.Contains("plugin.json", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_ExtraFiles_RejectedAndListed()
    {
        var package = CreatePackage(
            ("community/plugin.json", Text(ValidDescriptor)),
            ("community/payload.cmd", Text("evil")));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("payload.cmd", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_RootLevelFile_Rejected()
    {
        var package = CreatePackage(
            ("plugin.json", Text(ValidDescriptor)));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("top-level directory", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_MultipleTopLevelDirectories_Rejected()
    {
        var package = CreatePackage(
            ("community/plugin.json", Text(ValidDescriptor)),
            ("other/plugin.json", Text(ValidDescriptor.Replace("community", "other", StringComparison.Ordinal))));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("exactly one top-level directory", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_ParentRelativePath_RejectedAsZipSlip()
    {
        var package = CreatePackage(
            ("community/plugin.json", Text(ValidDescriptor)),
            ("../evil.txt", Text("payload")));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("unsafe path", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_AbsolutePath_RejectedAsZipSlip()
    {
        var package = CreatePackage(
            ("community/plugin.json", Text(ValidDescriptor)),
            ("/etc/evil.txt", Text("payload")));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("unsafe path", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_DriveAbsolutePath_RejectedAsZipSlip()
    {
        var package = CreatePackage(
            ("community/plugin.json", Text(ValidDescriptor)),
            (@"C:\evil.txt", Text("payload")));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("unsafe path", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_SymlinkEntry_Rejected()
    {
        var path = Path.Combine(Path.GetTempPath(), "clihub-package-" + Guid.NewGuid().ToString("N") + ".clihub-plugin.zip");
        try
        {
            using (var stream = File.Create(path))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                var descriptor = archive.CreateEntry("community/plugin.json");
                using (var destination = descriptor.Open())
                {
                    destination.Write(Encoding.UTF8.GetBytes(ValidDescriptor));
                }

                var symlink = archive.CreateEntry("community/logo.png");
                // Zip stores the Unix mode in the high 16 bits; 0xA1FF is a symlink mode (S_IFLNK).
                symlink.ExternalAttributes = 0xA1FF << 16;
            }

            var preview = this.CreateInstaller().Inspect(path);

            Assert.False(preview.IsValid);
            Assert.Contains(preview.Errors, error => error.Contains("symlink", StringComparison.Ordinal));
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    public void Inspect_HiddenFile_Rejected()
    {
        var package = CreatePackage(
            ("community/plugin.json", Text(ValidDescriptor)),
            ("community/.hidden", Text("payload")));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("hidden", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_ArchiveTooLarge_RejectedWithErrorIncludingSize()
    {
        var largeContent = new byte[11 * 1024 * 1024];
        var package = CreatePackage(
            ("community/plugin.json", Text(ValidDescriptor)),
            ("community/logo.png", largeContent));

        try
        {
            var preview = this.CreateInstaller().Inspect(package);

            Assert.False(preview.IsValid);
            Assert.Contains(preview.Errors, error => error.Contains("too large", StringComparison.Ordinal));
        }
        finally
        {
            try { File.Delete(package); } catch { }
        }
    }

    [Fact]
    public void Inspect_TooManyFiles_Rejected()
    {
        var entries = new List<(string, byte[])> { ("community/plugin.json", Text(ValidDescriptor)) };
        for (var i = 0; i < 101; i++)
        {
            entries.Add(($"community/file{i}.txt", Text("x")));
        }

        var package = CreatePackage(entries.ToArray());

        var preview = this.CreateInstaller().Inspect(package);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("more than 100 entries", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_IdMismatch_Rejected()
    {
        var package = CreatePackage(
            ("community/plugin.json", Text(ValidDescriptor.Replace("\"community\"", "\"renamed\"", StringComparison.Ordinal))));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("must match the package directory name", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_BuiltinId_RejectedAgainstEmbeddedResources()
    {
        Assert.Contains("opencode", EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly));

        var package = CreatePackage(
            ("opencode/plugin.json", Text(ValidDescriptor.Replace("community", "opencode", StringComparison.Ordinal))));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.False(preview.IsValid);
        Assert.Contains(preview.Errors, error => error.Contains("reserved", StringComparison.Ordinal));
    }

    [Fact]
    public void Inspect_FormerlyBuiltinClineCliId_NotRejectedAsReserved()
    {
        Assert.DoesNotContain("cline-cli", EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly));

        var package = CreatePackage(
            ("cline-cli/plugin.json", Text(ValidDescriptor.Replace("community", "cline-cli", StringComparison.Ordinal))));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.True(preview.IsValid);
        Assert.Empty(preview.Errors);
        Assert.Equal("cline-cli", preview.PluginId);
    }

    [Fact]
    public void Inspect_FormerlyBuiltinOpenClaudeId_NotRejectedAsReserved()
    {
        Assert.DoesNotContain("openclaude", EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly));

        var package = CreatePackage(
            ("openclaude/plugin.json", Text(ValidDescriptor.Replace("community", "openclaude", StringComparison.Ordinal))));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.True(preview.IsValid);
        Assert.Empty(preview.Errors);
        Assert.Equal("openclaude", preview.PluginId);
    }

    [Fact]
    public void Inspect_FormerlyBuiltinGithubCopilotId_NotRejectedAsReserved()
    {
        Assert.DoesNotContain("github-copilot", EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly));

        var package = CreatePackage(
            ("github-copilot/plugin.json", Text(ValidDescriptor.Replace("community", "github-copilot", StringComparison.Ordinal))));

        var preview = this.CreateInstaller().Inspect(package);

        Assert.True(preview.IsValid);
        Assert.Empty(preview.Errors);
        Assert.Equal("github-copilot", preview.PluginId);
    }

    [Fact]
    public void Inspect_ExistingInstalledPlugin_IdentifiesReplacementScenario()
    {
        this.WriteExistingPlugin("community", ValidDescriptor.Replace("1.2.3", "0.9.0", StringComparison.Ordinal), installedVersion: "0.9.0");

        var preview = this.CreateInstaller().Inspect(ValidPackage());

        Assert.True(preview.IsValid);
        Assert.True(preview.ReplacesExistingPlugin);
        Assert.Equal("0.9.0", preview.ExistingPluginVersion);
    }

    [Fact]
    public void Inspect_WritesNoFilesToPluginsOrStagingDirectories()
    {
        this.WriteExistingPlugin("community", ValidDescriptor);
        var repository = new PluginStateRepository(NullLogger<PluginStateRepository>.Instance, this.stateFilePath);
        repository.Save(repository.Load());
        var stateBefore = File.ReadAllText(this.stateFilePath);

        var preview = this.CreateInstaller().Inspect(ValidPackage());

        Assert.True(preview.IsValid);
        Assert.Equal(["community"], this.PluginRootSubdirectories());
        Assert.Equal(stateBefore, File.ReadAllText(this.stateFilePath));
    }

    [Fact]
    public void Install_ValidPackage_InstallsFilesUpdatesStateAndReloadsCatalog()
    {
        var installer = this.CreateInstaller();

        var result = installer.Install(ValidPackage(), replaceExisting: false);

        Assert.True(result.Success);
        Assert.Equal("community", result.InstalledPluginId);
        Assert.Empty(result.Errors);

        var pluginJson = Path.Combine(this.pluginsPath, "community", "plugin.json");
        var logo = Path.Combine(this.pluginsPath, "community", "logo.png");
        Assert.Equal(ValidDescriptor, File.ReadAllText(pluginJson));
        Assert.Equal(LogoBytes, File.ReadAllBytes(logo));

        var state = new PluginStateRepository(NullLogger<PluginStateRepository>.Instance, this.stateFilePath).Load();
        var entry = state.Plugins["community"];
        Assert.Equal(PluginOrigin.Installed, entry.Origin);
        Assert.Equal(PluginHasher.ComputeHash(ValidDescriptor), entry.ContentHash);
        Assert.Equal(PluginHasher.ComputeHash(LogoBytes), entry.LogoHash);
        Assert.Equal("1.2.3", entry.InstalledVersion);
        Assert.False(entry.Disabled);

        this.catalog.Verify(x => x.ReloadPlugins(), Times.Once);
    }

    [Fact]
    public void Install_ValidPackage_LeavesNoStagingDirectoryBehind()
    {
        this.CreateInstaller().Install(ValidPackage(), replaceExisting: false);

        Assert.DoesNotContain(this.PluginRootSubdirectories(), name => name.StartsWith(".staging-", StringComparison.Ordinal));
    }

    [Fact]
    public void Install_ReplacingExisting_LeavesNoBackupDirectoryBehind()
    {
        this.WriteExistingPlugin("community", ValidDescriptor.Replace("1.2.3", "0.9.0", StringComparison.Ordinal));

        var result = this.CreateInstaller().Install(ValidPackage(), replaceExisting: true);

        Assert.True(result.Success);
        Assert.Contains("1.2.3", File.ReadAllText(Path.Combine(this.pluginsPath, "community", "plugin.json")), StringComparison.Ordinal);
        Assert.DoesNotContain(this.PluginRootSubdirectories(), name => name.Contains(".backup-", StringComparison.Ordinal));
        Assert.DoesNotContain(this.PluginRootSubdirectories(), name => name.StartsWith(".staging-", StringComparison.Ordinal));
    }

    [Fact]
    public void Install_DuplicateWithoutReplacement_RejectedWithNoWrites()
    {
        this.WriteExistingPlugin("community", ValidDescriptor.Replace("1.2.3", "0.9.0", StringComparison.Ordinal));
        var before = File.ReadAllText(Path.Combine(this.pluginsPath, "community", "plugin.json"));

        var result = this.CreateInstaller().Install(ValidPackage(), replaceExisting: false);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("already exists", StringComparison.Ordinal));
        Assert.Equal(before, File.ReadAllText(Path.Combine(this.pluginsPath, "community", "plugin.json")));
        Assert.Equal(["community"], this.PluginRootSubdirectories());
        this.catalog.Verify(x => x.ReloadPlugins(), Times.Never);
    }

    [Fact]
    public void Install_InvalidDescriptor_RejectedWithNoWrites()
    {
        var descriptor = """
        {
          "id": "community",
          "name": "Community Agent",
          "commands": { "version": { "executable": "community" } }
        }
        """;
        var package = CreatePackage(("community/plugin.json", Text(descriptor)));

        var result = this.CreateInstaller().Install(package, replaceExisting: false);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("launch", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(this.pluginsPath, "community")));
        Assert.DoesNotContain(this.PluginRootSubdirectories(), name => name.StartsWith(".staging-", StringComparison.Ordinal));
    }

    [Fact]
    public void Install_DescriptorInvalidAfterExtraction_RolledBack()
    {
        // The reader accepts the descriptor in memory (inspection) but reports a read failure
        // from disk, exercising the post-extraction re-validation gate.
        var reader = new Mock<IPluginDescriptorReader>();
        reader.Setup(x => x.ReadContent(It.IsAny<string>()))
            .Returns(new PluginDescriptorReadResult(System.Text.Json.JsonSerializer.Deserialize<CLIHub.Core.Models.Plugin>(ValidDescriptor, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }), null));
        reader.Setup(x => x.Read(It.IsAny<string>()))
            .Returns(new PluginDescriptorReadResult(null, new InvalidOperationException("corrupted during extraction")));

        var result = this.CreateInstaller(reader.Object).Install(ValidPackage(), replaceExisting: false);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("corrupted during extraction", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(this.pluginsPath, "community")));
        Assert.DoesNotContain(this.PluginRootSubdirectories(), name => name.StartsWith(".staging-", StringComparison.Ordinal));
    }

    [Fact]
    public void Install_FailureAfterBackup_RestoresBackupAndRemovesStaging()
    {
        this.WriteExistingPlugin("community", ValidDescriptor.Replace("1.2.3", "0.9.0", StringComparison.Ordinal));
        var originalContent = File.ReadAllText(Path.Combine(this.pluginsPath, "community", "plugin.json"));
        var installer = this.CreateInstaller();
        installer.BeforeStagingMoveForTests = () => throw new InvalidOperationException("simulated move failure");

        var result = installer.Install(ValidPackage(), replaceExisting: true);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("simulated move failure", StringComparison.Ordinal));

        // The previous plugin is restored verbatim and no staging or backup directories remain.
        Assert.Equal(originalContent, File.ReadAllText(Path.Combine(this.pluginsPath, "community", "plugin.json")));
        Assert.Equal(["community"], this.PluginRootSubdirectories());
    }

    [Fact]
    public void Install_MissingPackageFile_Rejected()
    {
        var result = this.CreateInstaller().Install(Path.Combine(this.root, "missing.zip"), replaceExisting: false);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("not found", StringComparison.Ordinal));
    }
}
