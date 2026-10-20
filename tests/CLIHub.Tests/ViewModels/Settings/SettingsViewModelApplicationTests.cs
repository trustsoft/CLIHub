namespace CLIHub.ViewModels;

using System.IO;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Controls;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Windows;
using CLIHub.Core.Models;
using CLIHub.Updates;
using CLIHub.Settings;
using CLIHub.Launching;
using CLIHub.Lifetime;

public class SettingsViewModelApplicationTests
{
    [Fact]
    public void Cancel_DoesNotPersistAndRequestsClose()
    {
        var fixture = new Fixture();
        var closeRequested = false;
        fixture.ViewModel.RequestClose += (_, _) => closeRequested = true;

        fixture.ViewModel.CancelCommand.Execute(null);

        Assert.True(closeRequested);
        fixture.Preferences.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
    }

    [Fact]
    public void Load_DefaultAccent_SelectsSkyPreset()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        Assert.Equal("Sky", fixture.ViewModel.SelectedAccent.Name);
        Assert.False(fixture.ViewModel.SelectedAccent.IsCustom);
        Assert.Equal("Active: Sky", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void Load_GitBashUnresolved_MarksGitBashNotInstalled()
    {
        var fixture = new Fixture();
        fixture.RuntimeExecutables.Setup(x => x.Locate(RuntimeKind.GitBash)).Returns((string?)null);
        fixture.RuntimeExecutables
            .Setup(x => x.Locate(RuntimeKind.PowerShell7))
            .Returns(@"C:\Program Files\PowerShell\7\pwsh.exe");

        fixture.ViewModel.Load();

        var gitBash = fixture.ViewModel.Runtimes.Single(option => option.Kind == RuntimeKind.GitBash);
        Assert.False(gitBash.IsInstalled);
        Assert.Contains("not installed", gitBash.DisplayName);
        Assert.All(
            fixture.ViewModel.Runtimes.Where(option => option.Kind != RuntimeKind.GitBash),
            option => Assert.True(option.IsInstalled));
        Assert.Equal(RuntimeKind.PowerShell, fixture.ViewModel.SelectedRuntime.Kind);
    }

    [Fact]
    public void Load_GitBashResolved_LeavesAllRuntimesInstalled()
    {
        var fixture = new Fixture();
        fixture.RuntimeExecutables
            .Setup(x => x.Locate(RuntimeKind.GitBash))
            .Returns(@"C:\Program Files\Git\bin\bash.exe");
        fixture.RuntimeExecutables
            .Setup(x => x.Locate(RuntimeKind.PowerShell7))
            .Returns(@"C:\Program Files\PowerShell\7\pwsh.exe");

        fixture.ViewModel.Load();

        Assert.All(fixture.ViewModel.Runtimes, option => Assert.True(option.IsInstalled));
        var gitBash = fixture.ViewModel.Runtimes.Single(option => option.Kind == RuntimeKind.GitBash);
        Assert.Equal("Git Bash", gitBash.DisplayName);
        Assert.Equal(RuntimeKind.PowerShell, fixture.ViewModel.SelectedRuntime.Kind);
    }

    [Fact]
    public void Load_GitBashStoredButUnavailable_PreservesSelectionAndSaves()
    {
        var fixture = new Fixture();
        fixture.Preferences.Setup(x => x.Load()).Returns(new AppPreferences
        {
            DefaultRuntime = RuntimeKind.GitBash,
            Hotkey = "Ctrl+Shift+A",
            PathDisplayStyle = PathDisplayStyles.LeftTrimToken,
            CheckForUpdatesOnStartup = true,
            StartMinimized = true,
            AccentColor = AccentPalette.DefaultHex
        });
        fixture.RuntimeExecutables.Setup(x => x.Locate(RuntimeKind.GitBash)).Returns((string?)null);
        fixture.RuntimeExecutables
            .Setup(x => x.Locate(RuntimeKind.PowerShell7))
            .Returns(@"C:\Program Files\PowerShell\7\pwsh.exe");
        fixture.Applier.Setup(x => x.ApplyRuntime(RuntimeKind.GitBash));

        fixture.ViewModel.Load();

        Assert.Equal(RuntimeKind.GitBash, fixture.ViewModel.SelectedRuntime.Kind);
        var gitBash = fixture.ViewModel.Runtimes.Single(option => option.Kind == RuntimeKind.GitBash);
        Assert.False(gitBash.IsInstalled);

        fixture.ViewModel.SaveCommand.Execute(null);

        Assert.Null(fixture.ViewModel.ValidationError);
        Assert.Equal(RuntimeKind.GitBash, fixture.CapturedPreferences.DefaultRuntime);
    }

    [Fact]
    public void Load_PowerShell7Unresolved_MarksPowerShell7NotInstalled()
    {
        var fixture = new Fixture();
        fixture.RuntimeExecutables
            .Setup(x => x.Locate(RuntimeKind.GitBash))
            .Returns(@"C:\Program Files\Git\bin\bash.exe");
        fixture.RuntimeExecutables.Setup(x => x.Locate(RuntimeKind.PowerShell7)).Returns((string?)null);

        fixture.ViewModel.Load();

        var powerShell7 = fixture.ViewModel.Runtimes.Single(option => option.Kind == RuntimeKind.PowerShell7);
        Assert.False(powerShell7.IsInstalled);
        Assert.Contains("not installed", powerShell7.DisplayName);
        Assert.All(
            fixture.ViewModel.Runtimes.Where(option => option.Kind != RuntimeKind.PowerShell7),
            option => Assert.True(option.IsInstalled));
        Assert.Equal(RuntimeKind.PowerShell, fixture.ViewModel.SelectedRuntime.Kind);
    }

    [Fact]
    public void Load_PowerShell7StoredButUnavailable_PreservesSelectionAndSaves()
    {
        var fixture = new Fixture();
        fixture.Preferences.Setup(x => x.Load()).Returns(new AppPreferences
        {
            DefaultRuntime = RuntimeKind.PowerShell7,
            Hotkey = "Ctrl+Shift+A",
            PathDisplayStyle = PathDisplayStyles.LeftTrimToken,
            CheckForUpdatesOnStartup = true,
            StartMinimized = true,
            AccentColor = AccentPalette.DefaultHex
        });
        fixture.RuntimeExecutables
            .Setup(x => x.Locate(RuntimeKind.GitBash))
            .Returns(@"C:\Program Files\Git\bin\bash.exe");
        fixture.RuntimeExecutables.Setup(x => x.Locate(RuntimeKind.PowerShell7)).Returns((string?)null);
        fixture.Applier.Setup(x => x.ApplyRuntime(RuntimeKind.PowerShell7));

        fixture.ViewModel.Load();

        Assert.Equal(RuntimeKind.PowerShell7, fixture.ViewModel.SelectedRuntime.Kind);
        var powerShell7 = fixture.ViewModel.Runtimes.Single(option => option.Kind == RuntimeKind.PowerShell7);
        Assert.False(powerShell7.IsInstalled);

        fixture.ViewModel.SaveCommand.Execute(null);

        Assert.Null(fixture.ViewModel.ValidationError);
        Assert.Equal(RuntimeKind.PowerShell7, fixture.CapturedPreferences.DefaultRuntime);
    }

    [Fact]
    public void Load_GitBashAvailabilityChanges_ReevaluatesAndPreservesSelection()
    {
        var fixture = new Fixture();
        fixture.Preferences.Setup(x => x.Load()).Returns(new AppPreferences
        {
            DefaultRuntime = RuntimeKind.GitBash,
            Hotkey = "Ctrl+Shift+A",
            PathDisplayStyle = PathDisplayStyles.LeftTrimToken,
            CheckForUpdatesOnStartup = true,
            StartMinimized = true,
            AccentColor = AccentPalette.DefaultHex
        });
        fixture.RuntimeExecutables
            .SetupSequence(x => x.Locate(RuntimeKind.GitBash))
            .Returns(@"C:\Program Files\Git\bin\bash.exe")
            .Returns((string?)null);
        fixture.RuntimeExecutables
            .Setup(x => x.Locate(RuntimeKind.PowerShell7))
            .Returns(@"C:\Program Files\PowerShell\7\pwsh.exe");

        fixture.ViewModel.Load();

        var firstGitBash = fixture.ViewModel.Runtimes.Single(option => option.Kind == RuntimeKind.GitBash);
        Assert.True(firstGitBash.IsInstalled);
        Assert.Equal(RuntimeKind.GitBash, fixture.ViewModel.SelectedRuntime.Kind);

        fixture.ViewModel.Load();

        var secondGitBash = fixture.ViewModel.Runtimes.Single(option => option.Kind == RuntimeKind.GitBash);
        Assert.False(secondGitBash.IsInstalled);
        Assert.Equal(RuntimeKind.GitBash, fixture.ViewModel.SelectedRuntime.Kind);

        // WPF can push null through the TwoWay SelectedItem binding when the rebuilt list replaces
        // the selected instance; the last non-null selection must survive.
        fixture.ViewModel.SelectedRuntime = null!;
        Assert.Equal(RuntimeKind.GitBash, fixture.ViewModel.SelectedRuntime.Kind);
    }

    [Fact]
    public void Load_CustomStoredAccent_SelectsCustomEntry()
    {
        var fixture = new Fixture("#123456");
        fixture.ViewModel.Load();

        Assert.True(fixture.ViewModel.SelectedAccent.IsCustom);
        Assert.Equal("#123456", fixture.ViewModel.AccentText);
        Assert.Equal("Active: Custom", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void SelectedAccent_Preset_UpdatesTextAndLabel()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.Name == "Emerald");

        Assert.Equal("#10B981", fixture.ViewModel.AccentText);
        Assert.Equal("Active: Emerald", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void AccentText_CustomValue_SelectsCustomEntry()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.AccentText = "#123456";

        Assert.True(fixture.ViewModel.SelectedAccent.IsCustom);
        Assert.Equal("Active: Custom", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void AccentText_PartialValue_SelectsCustomEntry()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.AccentText = "#12";

        Assert.True(fixture.ViewModel.SelectedAccent.IsCustom);
    }

    [Fact]
    public void SelectedAccent_CustomEntry_KeepsCurrentColor()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#4CC2FF", fixture.ViewModel.AccentText);
        Assert.True(fixture.ViewModel.SelectedAccent.IsCustom);
        Assert.Equal("Active: Sky", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void SelectedAccent_CustomAfterPreset_RestoresLastCustomValue()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        fixture.ViewModel.AccentText = "#123456";

        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.Name == "Emerald");
        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#123456", fixture.ViewModel.AccentText);
        Assert.True(fixture.ViewModel.SelectedAccent.IsCustom);
        Assert.Equal("Active: Custom", fixture.ViewModel.ActiveAccentLabel);
    }

    [Fact]
    public void SelectedAccent_CustomEntry_RestoresCustomAcrossSettingsWindows()
    {
        var memory = new AccentSelectionMemory();
        var first = new Fixture(accentMemory: memory);
        first.ViewModel.Load();
        first.ViewModel.AccentText = "#123456";
        first.ViewModel.SelectedAccent = first.ViewModel.AccentOptions.First(choice => choice.Name == "Teal");

        var second = new Fixture(accentMemory: memory);
        second.ViewModel.Load();
        second.ViewModel.SelectedAccent = second.ViewModel.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#123456", second.ViewModel.AccentText);
    }

    [Fact]
    public void Load_CustomStoredAccent_RestoresWhenCustomReselected()
    {
        var fixture = new Fixture("#123456");
        fixture.ViewModel.Load();

        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.Name == "Emerald");
        fixture.ViewModel.SelectedAccent = fixture.ViewModel.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#123456", fixture.ViewModel.AccentText);
    }

    [Fact]
    public void AddWatchedDirectory_ExistingAbsolutePath_AddsRootAndClearsInput()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var path = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        fixture.ViewModel.NewWatchedDirectoryText = path;

        fixture.ViewModel.AddWatchedDirectoryCommand.Execute(null);

        Assert.Contains(fixture.ViewModel.WatchedDirectories, row => string.Equals(row.Path, path, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(string.Empty, fixture.ViewModel.NewWatchedDirectoryText);
        Assert.False(fixture.ViewModel.HasWatchedDirectoryError);
        Assert.Equal("1 root monitored", fixture.ViewModel.WatchedRootsSummary);
    }

    [Fact]
    public void AddWatchedDirectory_MissingFolder_ReportsError()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing");
        fixture.ViewModel.NewWatchedDirectoryText = path;

        fixture.ViewModel.AddWatchedDirectoryCommand.Execute(null);

        Assert.Empty(fixture.ViewModel.WatchedDirectories);
        Assert.True(fixture.ViewModel.HasWatchedDirectoryError);
    }

    [Fact]
    public void RemoveWatchedDirectoryCommand_RemovesRoot()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var path = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        fixture.ViewModel.NewWatchedDirectoryText = path;
        fixture.ViewModel.AddWatchedDirectoryCommand.Execute(null);

        fixture.ViewModel.RemoveWatchedDirectoryCommand.Execute(path);

        Assert.Empty(fixture.ViewModel.WatchedDirectories);
        Assert.Equal("No roots monitored", fixture.ViewModel.WatchedRootsSummary);
    }

    [Fact]
    public void RemoveIgnoredPatternCommand_RemovesPattern()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        fixture.ViewModel.NewIgnoredPatternText = "custom-ignored-dir";
        fixture.ViewModel.AddIgnoredPatternCommand.Execute(null);

        fixture.ViewModel.RemoveIgnoredPatternCommand.Execute("custom-ignored-dir");

        Assert.DoesNotContain("custom-ignored-dir", fixture.ViewModel.IgnoredPatterns);
    }

    [Fact]
    public void SelectedScanDepth_SetOption_UpdatesText()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var option = fixture.ViewModel.ScanDepthOptions.First(item => item.Value == 4);

        fixture.ViewModel.SelectedScanDepth = option;

        Assert.Equal("4", fixture.ViewModel.ScanDepthText);
    }

    [Fact]
    public void Load_DoesNotApplyScanningOrReloadScanner()
    {
        var fixture = new Fixture();

        fixture.ViewModel.Load();

        fixture.Preferences.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
        fixture.Scanner.Verify(x => x.Reload(), Times.Never);
    }

    [Fact]
    public void AddWatchedDirectory_AppliesImmediatelyAndReloadsScanner()
    {
        var directory = CreateTempDirectory();
        try
        {
            var fixture = new Fixture();
            fixture.ViewModel.Load();
            fixture.ViewModel.NewWatchedDirectoryText = directory;

            fixture.ViewModel.AddWatchedDirectoryCommand.Execute(null);

            Assert.Equal([directory], fixture.CapturedPreferences.WatchedDirectories ?? []);
            var settings = fixture.CapturedPreferences.WatchedDirectorySettings;
            Assert.NotNull(settings);
            Assert.True(settings.ContainsKey(directory));
            fixture.Scanner.Verify(x => x.Reload(), Times.Once);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemoveWatchedDirectory_AppliesImmediatelyAndReloadsScanner()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        fixture.ViewModel.WatchedDirectories.Add(CreateRow(fixture, @"C:\repos\one"));
        fixture.Scanner.Invocations.Clear();

        fixture.ViewModel.RemoveWatchedDirectoryCommand.Execute(@"C:\repos\one");

        Assert.Empty(fixture.ViewModel.WatchedDirectories);
        Assert.DoesNotContain(@"C:\repos\one", fixture.CapturedPreferences.WatchedDirectories ?? []);
        fixture.Scanner.Verify(x => x.Reload(), Times.Once);
    }

    [Fact]
    public void RowDepthChange_AppliesImmediatelyAndReloadsScanner()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var row = CreateRow(fixture, @"C:\repos\one");
        fixture.ViewModel.WatchedDirectories.Add(row);
        fixture.Scanner.Invocations.Clear();

        row.SelectedDepth = fixture.ViewModel.ScanDepthOptions.First(option => option.Value == 5);

        var settings = fixture.CapturedPreferences.WatchedDirectorySettings;
        Assert.NotNull(settings);
        Assert.Equal(5, settings[@"C:\repos\one"].ScanDepth);
        fixture.Scanner.Verify(x => x.Reload(), Times.Once);
    }

    [Fact]
    public void IgnoredPatternAdd_AppliesImmediatelyAndReloadsScanner()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        fixture.ViewModel.NewIgnoredPatternText = "custom-ignored-dir";

        fixture.ViewModel.AddIgnoredPatternCommand.Execute(null);

        Assert.Contains("custom-ignored-dir", fixture.ViewModel.IgnoredPatterns);
        Assert.Contains("custom-ignored-dir", fixture.CapturedPreferences.IgnoredPatterns ?? []);
        fixture.Scanner.Verify(x => x.Reload(), Times.Once);
    }

    [Fact]
    public void SelectedDiscoveryMode_Change_AppliesImmediately()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        fixture.Scanner.Invocations.Clear();

        fixture.ViewModel.SelectedDiscoveryMode = fixture.ViewModel.DiscoveryModes
            .First(option => option.Mode == ProjectDiscoveryMode.Git);

        Assert.Equal(ProjectDiscoveryMode.Git, fixture.CapturedPreferences.DiscoveryMode);
        fixture.Scanner.Verify(x => x.Reload(), Times.Once);
    }

    [Fact]
    public void SelectedScanDepth_Change_AppliesImmediately()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        fixture.Scanner.Invocations.Clear();

        fixture.ViewModel.SelectedScanDepth = fixture.ViewModel.ScanDepthOptions
            .First(option => option.Value == 4);

        Assert.Equal(4, fixture.CapturedPreferences.ScanDepth);
        fixture.Scanner.Verify(x => x.Reload(), Times.Once);
    }

    [Fact]
    public void WatchedDirectoryRow_DiscoveredCount_RefreshesOnScannerStatus()
    {
        const string directory = @"C:\repos\one";
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        fixture.Scanner.Setup(scanner => scanner.CountDiscoveredProjects(directory)).Returns(2);
        var row = new WatchedDirectoryRow(
            directory,
            fixture.ViewModel.ScanDepthOptions,
            fixture.ViewModel.DiscoveryModes,
            fixture.ViewModel.SelectedScanDepth,
            fixture.ViewModel.SelectedDiscoveryMode);

        fixture.ViewModel.WatchedDirectories.Add(row);

        Assert.Equal(2, row.DiscoveredCount);
        Assert.Equal("2 repositories", row.DiscoveredSummary);

        fixture.Scanner.Setup(scanner => scanner.CountDiscoveredProjects(directory)).Returns(1);
        fixture.Scanner.Raise(
            scanner => scanner.StatusChanged += null,
            fixture.Scanner.Object,
            new Core.Scanning.ProjectScanStatus(
                Core.Scanning.ProjectScanState.Idle,
                true,
                false,
                DateTimeOffset.UtcNow,
                1,
                0,
                null));

        Assert.Equal(1, row.DiscoveredCount);
        Assert.Equal("1 repository", row.DiscoveredSummary);
    }

    [Fact]
    public void WatchedDirectoryRow_Edit_DoesNotChangeAnotherRow()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var first = CreateRow(fixture, @"C:\repos\one");
        var second = CreateRow(fixture, @"C:\repos\two");
        fixture.ViewModel.WatchedDirectories.Add(first);
        fixture.ViewModel.WatchedDirectories.Add(second);

        first.SelectedDepth = fixture.ViewModel.ScanDepthOptions.First(option => option.Value == 5);
        first.SelectedDiscovery = fixture.ViewModel.DiscoveryModes.First(option => option.Mode == ProjectDiscoveryMode.Git);

        Assert.Equal(5, first.SelectedDepth.Value);
        Assert.Equal(ProjectScanningDefaults.DefaultScanDepth, second.SelectedDepth.Value);
        Assert.Equal(ProjectDiscoveryMode.Both, second.SelectedDiscovery.Mode);
    }

    [Fact]
    public void Load_PerRootSettings_ApplyToEachRow()
    {
        const string first = @"C:\repos\one";
        const string second = @"C:\repos\two";
        var fixture = new Fixture();
        fixture.Preferences.Setup(x => x.Load()).Returns(new AppPreferences
        {
            DefaultRuntime = RuntimeKind.PowerShell,
            Hotkey = "Ctrl+Shift+A",
            PathDisplayStyle = PathDisplayStyles.LeftTrimToken,
            CheckForUpdatesOnStartup = true,
            StartMinimized = true,
            AccentColor = AccentPalette.DefaultHex,
            WatchedDirectories = [first, second],
            WatchedDirectorySettings = new Dictionary<string, WatchedDirectoryScanSettings>
            {
                [first] = new(4, ProjectDiscoveryMode.Git)
            }
        });

        fixture.ViewModel.Load();

        var stored = fixture.ViewModel.WatchedDirectories.First(row => row.Path == first);
        Assert.Equal(4, stored.SelectedDepth.Value);
        Assert.Equal(ProjectDiscoveryMode.Git, stored.SelectedDiscovery.Mode);

        var fallback = fixture.ViewModel.WatchedDirectories.First(row => row.Path == second);
        Assert.Equal(ProjectScanningDefaults.DefaultScanDepth, fallback.SelectedDepth.Value);
        Assert.Equal(ProjectDiscoveryMode.Both, fallback.SelectedDiscovery.Mode);
    }

    [Fact]
    public void ScanDepthOptions_UsePlainLevelLabels()
    {
        var fixture = new Fixture();

        Assert.Equal(
            new[]
            {
                "1 level",
                "2 levels (default)",
                "3 levels",
                "4 levels",
                "5 levels",
                "6 levels",
                "7 levels",
                "8 levels"
            },
            fixture.ViewModel.ScanDepthOptions.Select(option => option.Name));
    }

    [Fact]
    public void Load_ScannerStatus_PopulatesSummary()
    {
        var fixture = new Fixture();
        fixture.Scanner.Setup(scanner => scanner.CountManualProjects()).Returns(4);
        fixture.Scanner.SetupGet(scanner => scanner.Status).Returns(
            new Core.Scanning.ProjectScanStatus(
                Core.Scanning.ProjectScanState.Idle,
                true,
                false,
                DateTimeOffset.UtcNow,
                7,
                42,
                null));

        fixture.ViewModel.Load();

        Assert.Equal(7, fixture.ViewModel.DiscoveredProjectCount);
        Assert.Equal(4, fixture.ViewModel.ManualProjectCount);
        Assert.Equal(11, fixture.ViewModel.TotalProjectCount);
        Assert.Equal("7 discovered · 4 manual", fixture.ViewModel.ProjectMixSummary);
        Assert.Equal(42, fixture.ViewModel.ScannedDirectoryCount);
        Assert.Equal("Watching", fixture.ViewModel.WatcherStateLabel);
        Assert.StartsWith("Last scan", fixture.ViewModel.WatcherDetailText);
        Assert.EndsWith("· 42 folders", fixture.ViewModel.WatcherDetailText);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, "· 1 folder")]
    [InlineData(412, "· 412 folders")]
    public void WatcherDetail_AppendsExaminedFolderCount(int count, string? expectedEnding)
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        fixture.Scanner.Raise(
            scanner => scanner.StatusChanged += null,
            fixture.Scanner.Object,
            new Core.Scanning.ProjectScanStatus(
                Core.Scanning.ProjectScanState.Idle,
                true,
                false,
                DateTimeOffset.UtcNow,
                0,
                count,
                null));

        Assert.Equal(count, fixture.ViewModel.ScannedDirectoryCount);

        if (expectedEnding is null)
        {
            Assert.StartsWith("Last scan", fixture.ViewModel.WatcherDetailText);
            Assert.DoesNotContain("·", fixture.ViewModel.WatcherDetailText);
            return;
        }

        Assert.EndsWith(expectedEnding, fixture.ViewModel.WatcherDetailText);
    }

    [Fact]
    public void ScanNowCommand_Execute_ReloadsScanner()
    {
        var fixture = new Fixture();

        fixture.ViewModel.ScanNowCommand.Execute(null);

        fixture.Scanner.Verify(scanner => scanner.Reload(), Times.Once);
    }

    [Fact]
    public void ScanWatchedDirectoryCommand_Execute_RescansThatRootOnly()
    {
        var fixture = new Fixture();
        var directory = @"C:\repos\one";

        fixture.ViewModel.ScanWatchedDirectoryCommand.Execute(directory);

        fixture.Scanner.Verify(scanner => scanner.Rescan(directory), Times.Once);
        fixture.Scanner.Verify(scanner => scanner.Reload(), Times.Never);
    }

    [Fact]
    public void AddWatchedDirectoryFromPicker_SelectedFolder_AddsRoot()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();
        var path = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        fixture.Dialogs.Setup(dialogs => dialogs.SelectFolder(It.IsAny<string>())).Returns(path);

        fixture.ViewModel.AddWatchedDirectoryFromPickerCommand.Execute(null);

        Assert.Contains(fixture.ViewModel.WatchedDirectories, row => string.Equals(row.Path, path, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Save_ValidDraft_PersistsAndRequestsClose()
    {
        var fixture = new Fixture();
        var closeRequested = false;
        fixture.ViewModel.RequestClose += (_, _) => closeRequested = true;
        fixture.ViewModel.Load();

        fixture.ViewModel.SaveCommand.Execute(null);

        Assert.True(closeRequested);
        Assert.Null(fixture.ViewModel.ValidationError);
        fixture.Preferences.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Once);
    }

    [Fact]
    public void Save_PerRowSettings_PersistsWithTheWatchedDirectory()
    {
        var fixture = new Fixture();
        AppPreferences? saved = null;
        fixture.Preferences
            .Setup(x => x.Update(It.IsAny<Action<AppPreferences>>()))
            .Callback<Action<AppPreferences>>(update =>
            {
                var preferences = new AppPreferences();
                update(preferences);
                saved = preferences;
            });
        fixture.ViewModel.Load();
        var path = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        fixture.ViewModel.NewWatchedDirectoryText = path;
        fixture.ViewModel.AddWatchedDirectoryCommand.Execute(null);
        var row = fixture.ViewModel.WatchedDirectories.Single(candidate => candidate.Path == path);
        row.SelectedDepth = fixture.ViewModel.ScanDepthOptions.First(option => option.Value == 4);
        row.SelectedDiscovery = fixture.ViewModel.DiscoveryModes.First(option => option.Mode == ProjectDiscoveryMode.Git);

        fixture.ViewModel.SaveCommand.Execute(null);

        Assert.NotNull(saved);
        Assert.True(saved!.WatchedDirectorySettings.TryGetValue(path, out var settings));
        Assert.Equal(4, settings!.ScanDepth);
        Assert.Equal(ProjectDiscoveryMode.Git, settings.DiscoveryMode);
    }

    [Fact]
    public void ShowReleaseNotesCommand_Execute_OpensReleaseNotes()
    {
        var fixture = new Fixture();

        fixture.ViewModel.ShowReleaseNotesCommand.Execute(null);

        fixture.ReleaseNotes.Verify(x => x.ShowReleaseNotes(), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_UpToDate_SetsCurrentStatus()
    {
        var fixture = new Fixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.UpToDate, "1.0.0", null));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("CLIHub is up to date", fixture.ViewModel.UpdateStatusTitle);
        Assert.Equal("Up to date", fixture.ViewModel.UpdateStatusBadge);
        Assert.Equal(SettingBadgeTone.Success, fixture.ViewModel.UpdateStatusBadgeTone);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_UpdateAvailable_IncludesAvailableVersion()
    {
        var fixture = new Fixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.UpdateAvailable, "1.0.0", "1.1.0"));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("Update available", fixture.ViewModel.UpdateStatusTitle);
        Assert.Equal("Update available", fixture.ViewModel.UpdateStatusBadge);
        Assert.Equal(SettingBadgeTone.Accent, fixture.ViewModel.UpdateStatusBadgeTone);
        Assert.Contains("1.1.0", fixture.ViewModel.UpdateStatusDetail);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_NotInstalled_SetsUnavailableStatus()
    {
        var fixture = new Fixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.NotInstalled, "1.0.0", null));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("Updates unavailable", fixture.ViewModel.UpdateStatusTitle);
        Assert.Equal("Not installed", fixture.ViewModel.UpdateStatusBadge);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_Failed_SetsFailureStatus()
    {
        var fixture = new Fixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.Failed, "1.0.0", null));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("Update check failed", fixture.ViewModel.UpdateStatusTitle);
        Assert.Equal("Failed", fixture.ViewModel.UpdateStatusBadge);
    }

    [Fact]
    public void Load_EmptyProbeFields_ShowsBuiltInDefaults()
    {
        var fixture = new Fixture();

        fixture.ViewModel.Load();

        Assert.Equal("15 min · 10 s", fixture.ViewModel.ProbePolicySummary);
        Assert.Equal("Built-in defaults", fixture.ViewModel.ProbePolicyDetail);
    }

    [Fact]
    public void ProbeFields_PositiveValues_ShowCustomEffectivePolicy()
    {
        var fixture = new Fixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.ProbeTtlText = "30";
        fixture.ViewModel.ProbeTimeoutText = "5";

        Assert.Equal("30 min · 5 s", fixture.ViewModel.ProbePolicySummary);
        Assert.Equal("Custom probe settings", fixture.ViewModel.ProbePolicyDetail);
    }

    [Fact]
    public void Save_InvalidDraft_DoesNotPersistOrClose()
    {
        var fixture = new Fixture();
        var closeRequested = false;
        fixture.ViewModel.RequestClose += (_, _) => closeRequested = true;
        fixture.ViewModel.Load();
        fixture.ViewModel.ProbeTimeoutText = "0";

        fixture.ViewModel.SaveCommand.Execute(null);

        Assert.False(closeRequested);
        Assert.Equal("Probe timeout must be a positive number of seconds, or empty for the default.", fixture.ViewModel.ValidationError);
        fixture.Preferences.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
    }

    private static WatchedDirectoryRow CreateRow(Fixture fixture, string path) =>
        new(path,
            fixture.ViewModel.ScanDepthOptions,
            fixture.ViewModel.DiscoveryModes,
            fixture.ViewModel.SelectedScanDepth,
            fixture.ViewModel.SelectedDiscoveryMode);

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "clihub-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class Fixture
    {
        public Mock<IPreferencesStore> Preferences { get; } = new(MockBehavior.Strict);
        public Mock<IStartupService> Startup { get; } = new(MockBehavior.Strict);
        public Mock<IPreferenceApplier> Applier { get; } = new(MockBehavior.Strict);
        public Mock<IUpdateWorkflow> Updates { get; } = new();
        public Mock<IReleaseNotesLauncher> ReleaseNotes { get; } = new();
        public Mock<IRuntimeExecutableLocator> RuntimeExecutables { get; } = new();
        public Mock<Core.Scanning.IProjectScanner> Scanner { get; } = new();
        public Mock<IProjectDialogService> Dialogs { get; } = new();
        public AccentSelectionMemory AccentMemory { get; }
        public AppPreferences CapturedPreferences { get; } = new();
        public SettingsViewModel ViewModel { get; }

        public Fixture(string accentColor = AccentPalette.DefaultHex, AccentSelectionMemory? accentMemory = null)
        {
            this.AccentMemory = accentMemory ?? new AccentSelectionMemory();
            this.Preferences.Setup(x => x.Load()).Returns(new AppPreferences
            {
                DefaultRuntime = RuntimeKind.PowerShell,
                Hotkey = "Ctrl+Shift+A",
                PathDisplayStyle = PathDisplayStyles.LeftTrimToken,
                CheckForUpdatesOnStartup = true,
                StartMinimized = true,
                AccentColor = accentColor
            });
            this.Preferences.Setup(x => x.Update(It.IsAny<Action<AppPreferences>>()))
                .Callback<Action<AppPreferences>>(update => update(this.CapturedPreferences));
            this.Startup.Setup(x => x.IsEnabled()).Returns(false);
            this.Applier.Setup(x => x.ApplyStartWithWindows(false)).Returns(true);
            this.Applier.Setup(x => x.ApplyRuntime(RuntimeKind.PowerShell));
            this.Applier.Setup(x => x.ApplyPathDisplayStyle(PathDisplayStyle.LeftTrim));
            this.Applier.Setup(x => x.ApplyHotkey(It.IsAny<CLIHub.Core.Hotkeys.HotkeyDefinition>())).Returns(true);
            this.Applier.Setup(x => x.ApplyStartupUpdateCheck(true));
            this.Applier.Setup(x => x.ApplyAccent(It.IsAny<string>()));
            this.Updates.Setup(x => x.GetCurrentVersion()).Returns("1.0.0");

            var application = new SettingsApplicationService(
                this.Preferences.Object,
                this.Startup.Object,
                this.Applier.Object,
                this.Scanner.Object,
                NullLogger<SettingsApplicationService>.Instance);
            this.ViewModel = new SettingsViewModel(
                application,
                this.Updates.Object,
                this.Updates.Object,
                this.ReleaseNotes.Object,
                new Mock<IApplicationOperationLifetime>().Object,
                new Mock<IExternalLauncher>().Object,
                this.Dialogs.Object,
                this.Scanner.Object,
                Mock.Of<IAgentManagementSection>(),
                Mock.Of<IProjectRegistrySection>(),
                this.AccentMemory,
                this.RuntimeExecutables.Object,
                NullLogger<SettingsViewModel>.Instance);
        }
    }
}
