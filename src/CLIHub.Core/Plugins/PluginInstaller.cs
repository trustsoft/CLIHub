namespace CLIHub.Core.Plugins;

using System.IO.Compression;
using System.Text;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Models;

/// <summary>
///   Installs agent plugins from local <c> .clihub-plugin.zip </c> packages. Inspection
///   validates the archive and parses the descriptor entirely in memory (no writes);
///   installation stages the extraction under a temporary <c> .staging-&lt;guid&gt; </c>
///   directory, moves it into place atomically with a backup of any replaced plugin, rolls
///   back on failure, records lifecycle state, and reloads the catalog. Only
///   <c> plugin.json </c> and <c> logo.png </c> may ever be written: no DLLs, no code
///   execution.
/// </summary>
public sealed class PluginInstaller : IPluginInstaller
{
    private const long MaxArchiveSizeBytes = 10 * 1024 * 1024;
    private const long MaxUncompressedEntrySizeBytes = 10 * 1024 * 1024;
    private const int MaxFileCount = 100;
    private const string DescriptorFileName = "plugin.json";
    private const string LogoFileName = "logo.png";
    private const string StagingPrefix = ".staging-";

    private readonly ILogger<PluginInstaller> logger;
    private readonly IPluginStateRepository stateRepository;
    private readonly IPluginCatalog catalog;
    private readonly IPluginDescriptorReader descriptorReader;
    private readonly IPluginDescriptorValidator descriptorValidator;
    private readonly string pluginsPath;

    internal Action? BeforeStagingMoveForTests { get; set; }

    /// <summary>
    ///   Creates the installer.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="stateRepository"> The plugin lifecycle state repository. </param>
    /// <param name="catalog"> The plugin catalog reloaded after a successful install. </param>
    /// <param name="descriptorReader"> The descriptor reader used to re-validate after extraction. </param>
    /// <param name="descriptorValidator"> The descriptor validator. </param>
    /// <param name="pluginsPath">
    ///   Overrides the plugins root; defaults to <c> %APPDATA%\CLIHub\plugins </c>.
    /// </param>
    public PluginInstaller(ILogger<PluginInstaller> logger,
                           IPluginStateRepository stateRepository,
                           IPluginCatalog catalog,
                           IPluginDescriptorReader? descriptorReader = null,
                           IPluginDescriptorValidator? descriptorValidator = null,
                           string? pluginsPath = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.stateRepository = stateRepository ?? throw new ArgumentNullException(nameof(stateRepository));
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.descriptorReader = descriptorReader ?? new PluginDescriptorReader();
        this.descriptorValidator = descriptorValidator ?? new PluginDescriptorValidator();
        this.pluginsPath = string.IsNullOrWhiteSpace(pluginsPath)
            ? AppPaths.PluginsDirectory
            : pluginsPath;
    }

    /// <inheritdoc />
    public PluginInstallPreview Inspect(string packagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        try
        {
            return this.InspectCore(packagePath);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Plugin package inspection failed for {Path}", packagePath);
            return PluginInstallPreview.Invalid([$"Package could not be read: {ex.Message}"]);
        }
    }

    /// <inheritdoc />
    public PluginInstallResult Install(string packagePath, bool replaceExisting)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        // Defense in depth: re-validate the package exactly as during inspection; the user may
        // have swapped the file between confirmation and install.
        var preview = this.InspectCore(packagePath);
        if (!preview.IsValid)
        {
            return PluginInstallResult.Failed(preview.Errors);
        }

        var pluginId = preview.PluginId!;
        var targetPath = Path.Combine(this.pluginsPath, pluginId);
        var existingPluginExists = this.PluginDirectoryExists(targetPath);

        if (existingPluginExists && !replaceExisting)
        {
            return PluginInstallResult.Failed(
                [$"An installed plugin with id '{pluginId}' already exists; choose replace to overwrite it"]);
        }

        var stagingPath = Path.Combine(this.pluginsPath, StagingPrefix + Guid.NewGuid().ToString("N"));
        string? backupPath = null;

        try
        {
            Directory.CreateDirectory(this.pluginsPath);
            Directory.CreateDirectory(stagingPath);

            using (var archive = ZipFile.OpenRead(packagePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (EntryIsDirectory(entry))
                    {
                        continue;
                    }

                    var fileName = GetFileName(entry.FullName);
                    if (fileName == DescriptorFileName)
                    {
                        WriteEntry(entry, Path.Combine(stagingPath, DescriptorFileName));
                    }
                    else if (fileName == LogoFileName)
                    {
                        WriteEntry(entry, Path.Combine(stagingPath, LogoFileName));
                    }
                }
            }

            if (!File.Exists(Path.Combine(stagingPath, DescriptorFileName)))
            {
                throw new PluginInstallException([$"Package is missing {DescriptorFileName}"]);
            }

            // Re-validate the extracted descriptor from disk through the standard reader and
            // validator path, so a package cannot install a descriptor the catalog would reject.
            var readResult = this.descriptorReader.Read(stagingPath);
            if (readResult.Error is not null || readResult.Plugin is null)
            {
                throw new PluginInstallException([$"Descriptor could not be read after extraction: {readResult.Error?.Message ?? "descriptor is empty"}"]);
            }

            var plugin = readResult.Plugin;
            var validationResult = this.descriptorValidator.Validate(plugin);
            if (!validationResult.IsValid)
            {
                throw new PluginInstallException(validationResult.Errors);
            }

            if (!string.Equals(plugin.Id, pluginId, StringComparison.Ordinal))
            {
                throw new PluginInstallException([$"Plugin id '{plugin.Id}' does not match the package directory name '{pluginId}'"]);
            }

            // Backup the existing plugin before replacing it, then move the staged directory
            // into place with a single rename.
            if (existingPluginExists)
            {
                backupPath = targetPath + ".backup-" + Guid.NewGuid().ToString("N");
                Directory.Move(targetPath, backupPath);
            }

            this.BeforeStagingMoveForTests?.Invoke();

            Directory.Move(stagingPath, targetPath);

            if (backupPath is not null)
            {
                TryDeleteDirectory(backupPath);
            }

            this.RecordInstalledState(targetPath, plugin);
            this.logger.LogInformation("Installed plugin {PluginId} from {Path}", pluginId, packagePath);

            // The install is committed: a reload failure must not roll the files back. The
            // catalog reload refreshes the UI; any next reload picks the plugin up as well.
            try
            {
                this.catalog.ReloadPlugins();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Catalog reload after installing {PluginId} failed", pluginId);
            }

            return PluginInstallResult.Succeeded(pluginId);
        }
        catch (Exception ex)
        {
            this.Rollback(stagingPath, targetPath, backupPath);
            var errors = ex is PluginInstallException installException
                ? installException.Errors
                : [$"Installation failed: {ex.Message}"];
            this.logger.LogWarning(ex, "Plugin installation failed for {Path}", packagePath);
            return PluginInstallResult.Failed(errors);
        }
    }

    private PluginInstallPreview InspectCore(string packagePath)
    {
        var errors = new List<string>();

        if (!File.Exists(packagePath))
        {
            return PluginInstallPreview.Invalid([$"Package file not found: {packagePath}"]);
        }

        var archiveSize = new FileInfo(packagePath).Length;
        if (archiveSize > MaxArchiveSizeBytes)
        {
            errors.Add($"Package is too large ({archiveSize:N0} bytes); the maximum is 10 MB");
            return PluginInstallPreview.Invalid(errors);
        }

        string? topDirectory = null;
        string? descriptorJson = null;
        var fileCount = 0;
        var topDirectories = new HashSet<string>(StringComparer.Ordinal);
        var disallowedFiles = new List<string>();

        using (var archive = ZipFile.OpenRead(packagePath))
        {
            foreach (var entry in archive.Entries)
            {
                if (EntryIsDirectory(entry))
                {
                    continue;
                }

                fileCount++;
                if (fileCount > MaxFileCount)
                {
                    errors.Add($"Package contains more than {MaxFileCount} entries");
                    return PluginInstallPreview.Invalid(errors);
                }

                var fullName = entry.FullName;

                if (IsSymlink(entry))
                {
                    errors.Add($"Package entry '{fullName}' is a symlink; symlinks are not allowed");
                    continue;
                }

                if (PathIsUnsafe(fullName))
                {
                    errors.Add($"Package entry '{fullName}' uses an unsafe path (zip-slip or absolute path)");
                    continue;
                }

                var segments = SplitSegments(fullName).ToArray();
                if (segments.Length < 2)
                {
                    errors.Add($"Package entry '{fullName}' is not inside a top-level directory");
                    continue;
                }

                topDirectories.Add(segments[0]);

                if (segments.Any(s => s.StartsWith(".", StringComparison.Ordinal)))
                {
                    errors.Add($"Package entry '{fullName}' is hidden; hidden files are not allowed");
                    continue;
                }

                if (!string.Equals(segments[1], DescriptorFileName, StringComparison.Ordinal)
                    && !string.Equals(segments[1], LogoFileName, StringComparison.Ordinal))
                {
                    disallowedFiles.Add(fullName);
                    continue;
                }

                if (entry.Length > MaxUncompressedEntrySizeBytes)
                {
                    errors.Add($"Package entry '{fullName}' is too large when extracted; the maximum is 10 MB");
                    continue;
                }

                if (string.Equals(segments[1], DescriptorFileName, StringComparison.Ordinal))
                {
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    descriptorJson = reader.ReadToEnd();
                }
            }
        }

        if (topDirectories.Count == 0)
        {
            errors.Add("Package must contain exactly one top-level directory; found none");
        }
        else if (topDirectories.Count > 1)
        {
            errors.Add($"Package must contain exactly one top-level directory; found {topDirectories.Count} ({string.Join(", ", topDirectories.OrderBy(d => d, StringComparer.Ordinal))})");
        }
        else
        {
            topDirectory = topDirectories.Single();
        }

        if (disallowedFiles.Count > 0)
        {
            errors.Add($"Package contains files other than {DescriptorFileName} and {LogoFileName}: {string.Join(", ", disallowedFiles)}");
        }

        if (topDirectory is not null && descriptorJson is null)
        {
            errors.Add($"Package is missing {topDirectory}/{DescriptorFileName}");
        }

        if (errors.Count > 0)
        {
            return PluginInstallPreview.Invalid(errors);
        }

        // Parse and validate the descriptor entirely in memory: no file is written during
        // inspection.
        var readResult = this.descriptorReader.ReadContent(descriptorJson!);
        if (readResult.Error is not null)
        {
            return PluginInstallPreview.Invalid([$"Descriptor could not be read: {readResult.Error.Message}"]);
        }

        var plugin = readResult.Plugin;
        if (plugin is null)
        {
            return PluginInstallPreview.Invalid(["Descriptor is empty"]);
        }

        var validationResult = this.descriptorValidator.Validate(plugin);
        errors.AddRange(validationResult.Errors);

        if (!string.Equals(plugin.Id, topDirectory, StringComparison.Ordinal))
        {
            errors.Add($"Plugin id '{plugin.Id}' must match the package directory name '{topDirectory}'");
        }

        if (EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly)
            .Contains(plugin.Id, StringComparer.Ordinal))
        {
            errors.Add($"Plugin id '{plugin.Id}' is reserved for the built-in agent '{plugin.Id}' and cannot be installed");
        }

        if (errors.Count > 0)
        {
            return new PluginInstallPreview(
                false,
                plugin.Id,
                plugin.Name,
                plugin.Version,
                plugin.Publisher,
                plugin.Homepage,
                BuildDeclaredCommands(plugin),
                false,
                null,
                errors);
        }

        var targetPath = Path.Combine(this.pluginsPath, plugin.Id);
        var replacesExisting = this.PluginDirectoryExists(targetPath);
        var existingVersion = replacesExisting
            ? this.stateRepository.Load().Plugins.GetValueOrDefault(plugin.Id)?.InstalledVersion
            : null;

        return new PluginInstallPreview(
            true,
            plugin.Id,
            plugin.Name,
            plugin.Version,
            plugin.Publisher,
            plugin.Homepage,
            BuildDeclaredCommands(plugin),
            replacesExisting,
            existingVersion,
            []);
    }

    private void RecordInstalledState(string pluginDirectory, Plugin plugin)
    {
        var document = this.stateRepository.Load();
        document.Plugins[plugin.Id] = new PluginStateEntry(PluginOrigin.Installed)
        {
            ContentHash = PluginHasher.ComputeFileHash(Path.Combine(pluginDirectory, DescriptorFileName)),
            LogoHash = PluginHasher.ComputeFileHash(Path.Combine(pluginDirectory, LogoFileName)),
            InstalledVersion = plugin.Version,
            Disabled = false
        };
        this.stateRepository.Save(document);
    }

    private void Rollback(string stagingPath, string targetPath, string? backupPath)
    {
        try
        {
            if (Directory.Exists(stagingPath))
            {
                Directory.Delete(stagingPath, recursive: true);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not remove the staging directory {Path}", stagingPath);
        }

        if (backupPath is null || !Directory.Exists(backupPath))
        {
            return;
        }

        try
        {
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, recursive: true);
            }

            Directory.Move(backupPath, targetPath);
            this.logger.LogInformation("Restored the previous plugin from backup to {Path}", targetPath);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not restore the plugin backup {Path}", backupPath);
        }
    }

    private static IReadOnlyList<CommandPreview> BuildDeclaredCommands(Plugin plugin)
    {
        var commands = new List<CommandPreview>();

        if (plugin.Commands?.Launch is { } launch)
        {
            commands.Add(new CommandPreview(nameof(AgentCommandKind.Launch).ToLowerInvariant(), launch.Executable, launch.Arguments));
        }

        if (plugin.Commands?.Resume is { } resume)
        {
            commands.Add(new CommandPreview(nameof(AgentCommandKind.Resume).ToLowerInvariant(), resume.Executable, resume.Arguments));
        }

        if (plugin.Commands?.Version is { } version)
        {
            commands.Add(new CommandPreview(nameof(AgentCommandKind.Version).ToLowerInvariant(), version.Executable, version.Arguments));
        }

        if (plugin.Commands?.Update is { } update)
        {
            commands.Add(new CommandPreview(nameof(AgentCommandKind.Update).ToLowerInvariant(), update.Executable, update.Arguments));
        }

        return commands;
    }

    private bool PluginDirectoryExists(string targetPath) =>
        Directory.Exists(targetPath) && File.Exists(Path.Combine(targetPath, DescriptorFileName));

    private static bool EntryIsDirectory(ZipArchiveEntry entry) =>
        entry.FullName.EndsWith("/", StringComparison.Ordinal)
        || entry.FullName.EndsWith("\\", StringComparison.Ordinal);

    private static bool IsSymlink(ZipArchiveEntry entry)
    {
        // Zip stores the Unix mode in the high 16 bits of ExternalAttributes; 0xA000 is the
        // symlink file-type nibble (S_IFLNK).
        const int fileTypeMask = 0xF000;
        const int symlinkFileType = 0xA000;
        return ((entry.ExternalAttributes >> 16) & fileTypeMask) == symlinkFileType;
    }

    private static bool PathIsUnsafe(string fullName) =>
        fullName.StartsWith("/", StringComparison.Ordinal)
        || fullName.StartsWith("\\", StringComparison.Ordinal)
        || (fullName.Length >= 2 && fullName[1] == ':')
        || SplitSegments(fullName).Any(s => s == "..");

    private static IEnumerable<string> SplitSegments(string fullName) =>
        fullName.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string GetFileName(string fullName) =>
        SplitSegments(fullName).LastOrDefault() ?? string.Empty;

    private static void WriteEntry(ZipArchiveEntry entry, string destinationPath)
    {
        using var source = entry.Open();
        using var destination = File.Create(destinationPath);
        source.CopyTo(destination);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // A leftover backup directory is harmless; the next install with the same id
            // replaces the plugin again.
        }
    }

    private sealed class PluginInstallException : Exception
    {
        public PluginInstallException(IReadOnlyList<string> errors)
            : base(string.Join("; ", errors))
        {
            this.Errors = errors;
        }

        public IReadOnlyList<string> Errors { get; }
    }
}
