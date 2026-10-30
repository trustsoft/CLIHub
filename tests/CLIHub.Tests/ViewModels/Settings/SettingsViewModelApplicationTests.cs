namespace CLIHub.ViewModels;

using System.ComponentModel;
using System.IO;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Controls;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Windows;
using CLIHub.Core.Models;
using CLIHub.Launching;
using CLIHub.Lifetime;
using CLIHub.Settings;
using CLIHub.Tests.Builders;
using CLIHub.Updates;

public class SettingsViewModelApplicationTests
{
    [Fact]
    public void Cancel_DoesNotPersistAndRequestsClose()
    {
        var fixture = new SettingsViewModelFixture();
        var closeRequested = false;
        fixture.ViewModel.RequestClose += (_, _) => closeRequested = true;

        fixture.ViewModel.CancelCommand.Execute(null);

        Assert.True(closeRequested);
        fixture.Preferences.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
    }

    [Fact]
    public void Load_DefaultAccent_SelectsSkyPreset()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();

        Assert.Equal("Sky", fixture.ViewModel.Accent.SelectedAccent.Name);
        Assert.False(fixture.ViewModel.Accent.SelectedAccent.IsCustom);
        Assert.Equal("Active: Sky", fixture.ViewModel.Accent.ActiveAccentLabel);
    }

    [Fact]
    public void Load_GitBashUnresolved_MarksGitBashNotInstalled()
    {
        var fixture = new SettingsViewModelFixture();
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
        var fixture = new SettingsViewModelFixture();
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
        var fixture = new SettingsViewModelFixture();
        fixture.Preferences.Setup(x => x.Load()).Returns(SettingsViewModelFixture.CreatePreferences(defaultRuntime: RuntimeKind.GitBash));
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
        var fixture = new SettingsViewModelFixture();
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
        var fixture = new SettingsViewModelFixture();
        fixture.Preferences.Setup(x => x.Load()).Returns(SettingsViewModelFixture.CreatePreferences(defaultRuntime: RuntimeKind.PowerShell7));
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
        var fixture = new SettingsViewModelFixture();
        fixture.Preferences.Setup(x => x.Load()).Returns(SettingsViewModelFixture.CreatePreferences(defaultRuntime: RuntimeKind.GitBash));
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
        var fixture = new SettingsViewModelFixture("#123456");
        fixture.ViewModel.Load();

        Assert.True(fixture.ViewModel.Accent.SelectedAccent.IsCustom);
        Assert.Equal("#123456", fixture.ViewModel.Accent.AccentText);
        Assert.Equal("Active: Custom", fixture.ViewModel.Accent.ActiveAccentLabel);
    }

    [Fact]
    public void SelectedAccent_Preset_UpdatesTextAndLabel()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.Accent.SelectedAccent = fixture.ViewModel.Accent.AccentOptions.First(choice => choice.Name == "Emerald");

        Assert.Equal("#10B981", fixture.ViewModel.Accent.AccentText);
        Assert.Equal("Active: Emerald", fixture.ViewModel.Accent.ActiveAccentLabel);
    }

    [Fact]
    public void AccentText_CustomValue_SelectsCustomEntry()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.Accent.AccentText = "#123456";

        Assert.True(fixture.ViewModel.Accent.SelectedAccent.IsCustom);
        Assert.Equal("Active: Custom", fixture.ViewModel.Accent.ActiveAccentLabel);
    }

    [Fact]
    public void AccentText_PartialValue_SelectsCustomEntry()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.Accent.AccentText = "#12";

        Assert.True(fixture.ViewModel.Accent.SelectedAccent.IsCustom);
    }

    [Fact]
    public void SelectedAccent_CustomEntry_KeepsCurrentColor()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.Accent.SelectedAccent = fixture.ViewModel.Accent.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#4CC2FF", fixture.ViewModel.Accent.AccentText);
        Assert.True(fixture.ViewModel.Accent.SelectedAccent.IsCustom);
        Assert.Equal("Active: Sky", fixture.ViewModel.Accent.ActiveAccentLabel);
    }

    [Fact]
    public void SelectedAccent_CustomAfterPreset_RestoresLastCustomValue()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        fixture.ViewModel.Accent.AccentText = "#123456";

        fixture.ViewModel.Accent.SelectedAccent = fixture.ViewModel.Accent.AccentOptions.First(choice => choice.Name == "Emerald");
        fixture.ViewModel.Accent.SelectedAccent = fixture.ViewModel.Accent.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#123456", fixture.ViewModel.Accent.AccentText);
        Assert.True(fixture.ViewModel.Accent.SelectedAccent.IsCustom);
        Assert.Equal("Active: Custom", fixture.ViewModel.Accent.ActiveAccentLabel);
    }

    [Fact]
    public void SelectedAccent_CustomEntry_RestoresCustomAcrossSettingsWindows()
    {
        var memory = new AccentSelectionMemory();
        var first = new SettingsViewModelFixture(accentMemory: memory);
        first.ViewModel.Load();
        first.ViewModel.Accent.AccentText = "#123456";
        first.ViewModel.Accent.SelectedAccent = first.ViewModel.Accent.AccentOptions.First(choice => choice.Name == "Teal");

        var second = new SettingsViewModelFixture(accentMemory: memory);
        second.ViewModel.Load();
        second.ViewModel.Accent.SelectedAccent = second.ViewModel.Accent.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#123456", second.ViewModel.Accent.AccentText);
    }

    [Fact]
    public void Load_CustomStoredAccent_RestoresWhenCustomReselected()
    {
        var fixture = new SettingsViewModelFixture("#123456");
        fixture.ViewModel.Load();

        fixture.ViewModel.Accent.SelectedAccent = fixture.ViewModel.Accent.AccentOptions.First(choice => choice.Name == "Emerald");
        fixture.ViewModel.Accent.SelectedAccent = fixture.ViewModel.Accent.AccentOptions.First(choice => choice.IsCustom);

        Assert.Equal("#123456", fixture.ViewModel.Accent.AccentText);
    }

    [Fact]
    public void AddWatchedDirectory_ExistingAbsolutePath_AddsRootAndClearsInput()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        var path = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        fixture.ViewModel.Scanning.NewWatchedDirectoryText = path;

        fixture.ViewModel.Scanning.AddWatchedDirectoryCommand.Execute(null);

        Assert.Contains(fixture.ViewModel.Scanning.WatchedDirectories, row => string.Equals(row.Path, path, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(string.Empty, fixture.ViewModel.Scanning.NewWatchedDirectoryText);
        Assert.False(fixture.ViewModel.Scanning.HasWatchedDirectoryError);
        Assert.Equal("1 root monitored", fixture.ViewModel.Scanning.WatchedRootsSummary);
    }

    [Fact]
    public void AddWatchedDirectory_MissingFolder_ReportsError()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing");
        fixture.ViewModel.Scanning.NewWatchedDirectoryText = path;

        fixture.ViewModel.Scanning.AddWatchedDirectoryCommand.Execute(null);

        Assert.Empty(fixture.ViewModel.Scanning.WatchedDirectories);
        Assert.True(fixture.ViewModel.Scanning.HasWatchedDirectoryError);
    }

    [Fact]
    public void AddWatchedDirectory_ApplyFailure_ReportsErrorAndKeepsInput()
    {
        var directory = CreateTempDirectory();
        try
        {
            var fixture = new SettingsViewModelFixture();
            fixture.Preferences
                .Setup(x => x.Update(It.IsAny<Action<AppPreferences>>()))
                .Throws(new IOException("configuration is read-only"));
            fixture.ViewModel.Load();
            fixture.ViewModel.Scanning.NewWatchedDirectoryText = directory;

            fixture.ViewModel.Scanning.AddWatchedDirectoryCommand.Execute(null);

            Assert.Empty(fixture.ViewModel.Scanning.WatchedDirectories);
            Assert.NotNull(fixture.ViewModel.ValidationError);
            Assert.Contains("Could not apply scanning settings", fixture.ViewModel.ValidationError);
            Assert.Equal(directory, fixture.ViewModel.Scanning.NewWatchedDirectoryText);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RemoveWatchedDirectoryCommand_RemovesRoot()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        var path = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        fixture.ViewModel.Scanning.NewWatchedDirectoryText = path;
        fixture.ViewModel.Scanning.AddWatchedDirectoryCommand.Execute(null);

        fixture.ViewModel.Scanning.RemoveWatchedDirectoryCommand.Execute(path);

        Assert.Empty(fixture.ViewModel.Scanning.WatchedDirectories);
        Assert.Equal("No roots monitored", fixture.ViewModel.Scanning.WatchedRootsSummary);
    }

    [Fact]
    public void RemoveIgnoredPatternCommand_RemovesPattern()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        fixture.ViewModel.Scanning.NewIgnoredPatternText = "custom-ignored-dir";
        fixture.ViewModel.Scanning.AddIgnoredPatternCommand.Execute(null);

        fixture.ViewModel.Scanning.RemoveIgnoredPatternCommand.Execute("custom-ignored-dir");

        Assert.DoesNotContain("custom-ignored-dir", fixture.ViewModel.Scanning.IgnoredPatterns);
    }

    [Fact]
    public void SelectedScanDepth_SetOption_UpdatesText()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        var option = fixture.ViewModel.Scanning.ScanDepthOptions.First(item => item.Value == 4);

        fixture.ViewModel.Scanning.SelectedScanDepth = option;

        Assert.Equal("4", fixture.ViewModel.Scanning.ScanDepthText);
    }

    [Fact]
    public void Load_DoesNotApplyScanningOrReloadScanner()
    {
        var fixture = new SettingsViewModelFixture();

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
            var fixture = new SettingsViewModelFixture();
            fixture.ViewModel.Load();
            fixture.ViewModel.Scanning.NewWatchedDirectoryText = directory;

            fixture.ViewModel.Scanning.AddWatchedDirectoryCommand.Execute(null);

            Assert.Equal([directory], fixture.CapturedPreferences.Scanning.WatchedDirectories ?? []);
            var settings = fixture.CapturedPreferences.Scanning.WatchedDirectorySettings;
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
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        fixture.ViewModel.Scanning.WatchedDirectories.Add(CreateRow(fixture, @"C:\repos\one"));
        fixture.Scanner.Invocations.Clear();

        fixture.ViewModel.Scanning.RemoveWatchedDirectoryCommand.Execute(@"C:\repos\one");

        Assert.Empty(fixture.ViewModel.Scanning.WatchedDirectories);
            Assert.DoesNotContain(@"C:\repos\one", fixture.CapturedPreferences.Scanning.WatchedDirectories ?? []);
        fixture.Scanner.Verify(x => x.Reload(), Times.Once);
    }

    [Fact]
    public void RowDepthChange_AppliesImmediatelyAndReloadsScanner()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        var row = CreateRow(fixture, @"C:\repos\one");
        fixture.ViewModel.Scanning.WatchedDirectories.Add(row);
        fixture.Scanner.Invocations.Clear();

        row.SelectedDepth = fixture.ViewModel.Scanning.ScanDepthOptions.First(option => option.Value == 5);

            var settings = fixture.CapturedPreferences.Scanning.WatchedDirectorySettings;
        Assert.NotNull(settings);
        Assert.Equal(5, settings[@"C:\repos\one"].ScanDepth);
        fixture.Scanner.Verify(x => x.Reload(), Times.Once);
    }

    [Fact]
    public void IgnoredPatternAdd_AppliesImmediatelyAndReloadsScanner()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        fixture.ViewModel.Scanning.NewIgnoredPatternText = "custom-ignored-dir";

        fixture.ViewModel.Scanning.AddIgnoredPatternCommand.Execute(null);

        Assert.Contains("custom-ignored-dir", fixture.ViewModel.Scanning.IgnoredPatterns);
            Assert.Contains("custom-ignored-dir", fixture.CapturedPreferences.Scanning.IgnoredPatterns ?? []);
        fixture.Scanner.Verify(x => x.Reload(), Times.Once);
    }

    [Fact]
    public void SelectedDiscoveryMode_Change_AppliesImmediately()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        fixture.Scanner.Invocations.Clear();

        fixture.ViewModel.Scanning.SelectedDiscoveryMode = fixture.ViewModel.Scanning.DiscoveryModes
            .First(option => option.Mode == ProjectDiscoveryMode.Git);

            Assert.Equal(ProjectDiscoveryMode.Git, fixture.CapturedPreferences.Scanning.DiscoveryMode);
        fixture.Scanner.Verify(x => x.Reload(), Times.Once);
    }

    [Fact]
    public void SelectedScanDepth_Change_AppliesImmediately()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        fixture.Scanner.Invocations.Clear();

        fixture.ViewModel.Scanning.SelectedScanDepth = fixture.ViewModel.Scanning.ScanDepthOptions
            .First(option => option.Value == 4);

            Assert.Equal(4, fixture.CapturedPreferences.Scanning.ScanDepth);
        fixture.Scanner.Verify(x => x.Reload(), Times.Once);
    }

    [Fact]
    public void WatchedDirectoryRow_DiscoveredCount_RefreshesOnScannerStatus()
    {
        const string directory = @"C:\repos\one";
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        fixture.Scanner.Setup(scanner => scanner.CountDiscoveredProjects(directory)).Returns(2);
        var row = new WatchedDirectoryRow(
            directory,
            fixture.ViewModel.Scanning.ScanDepthOptions,
            fixture.ViewModel.Scanning.DiscoveryModes,
            fixture.ViewModel.Scanning.SelectedScanDepth,
            fixture.ViewModel.Scanning.SelectedDiscoveryMode);

        fixture.ViewModel.Scanning.WatchedDirectories.Add(row);

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
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        var first = CreateRow(fixture, @"C:\repos\one");
        var second = CreateRow(fixture, @"C:\repos\two");
        fixture.ViewModel.Scanning.WatchedDirectories.Add(first);
        fixture.ViewModel.Scanning.WatchedDirectories.Add(second);

        first.SelectedDepth = fixture.ViewModel.Scanning.ScanDepthOptions.First(option => option.Value == 5);
        first.SelectedDiscovery = fixture.ViewModel.Scanning.DiscoveryModes.First(option => option.Mode == ProjectDiscoveryMode.Git);

        Assert.Equal(5, first.SelectedDepth.Value);
        Assert.Equal(ProjectScanningDefaults.DefaultScanDepth, second.SelectedDepth.Value);
        Assert.Equal(ProjectDiscoveryMode.Both, second.SelectedDiscovery.Mode);
    }

    [Fact]
    public void Load_PerRootSettings_ApplyToEachRow()
    {
        const string first = @"C:\repos\one";
        const string second = @"C:\repos\two";
        var fixture = new SettingsViewModelFixture();
        var preferences = SettingsViewModelFixture.CreatePreferences();
        preferences.Scanning = new ScanningPreferences
        {
            WatchedDirectories = [first, second],
            WatchedDirectorySettings = new Dictionary<string, WatchedDirectoryScanSettings>
            {
                [first] = new(4, ProjectDiscoveryMode.Git)
            }
        };
        fixture.Preferences.Setup(x => x.Load()).Returns(preferences);

        fixture.ViewModel.Load();

        var stored = fixture.ViewModel.Scanning.WatchedDirectories.First(row => row.Path == first);
        Assert.Equal(4, stored.SelectedDepth.Value);
        Assert.Equal(ProjectDiscoveryMode.Git, stored.SelectedDiscovery.Mode);

        var fallback = fixture.ViewModel.Scanning.WatchedDirectories.First(row => row.Path == second);
        Assert.Equal(ProjectScanningDefaults.DefaultScanDepth, fallback.SelectedDepth.Value);
        Assert.Equal(ProjectDiscoveryMode.Both, fallback.SelectedDiscovery.Mode);
    }

    [Fact]
    public void ScanDepthOptions_UsePlainLevelLabels()
    {
        var fixture = new SettingsViewModelFixture();

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
            fixture.ViewModel.Scanning.ScanDepthOptions.Select(option => option.Name));
    }

    [Fact]
    public void Load_ScannerStatus_PopulatesSummary()
    {
        var fixture = new SettingsViewModelFixture();
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

        Assert.Equal(7, fixture.ViewModel.Scanning.DiscoveredProjectCount);
        Assert.Equal(4, fixture.ViewModel.Scanning.ManualProjectCount);
        Assert.Equal(11, fixture.ViewModel.Scanning.TotalProjectCount);
        Assert.Equal("7 discovered · 4 manual", fixture.ViewModel.Scanning.ProjectMixSummary);
        Assert.Equal(42, fixture.ViewModel.Scanning.ScannedDirectoryCount);
        Assert.Equal("Watching", fixture.ViewModel.Scanning.WatcherStateLabel);
        Assert.StartsWith("Last scan", fixture.ViewModel.Scanning.WatcherDetailText);
        Assert.EndsWith("· 42 folders", fixture.ViewModel.Scanning.WatcherDetailText);
    }

    [Theory]
    [InlineData(Core.Scanning.ProjectScanState.NotConfigured, false, "Off")]
    [InlineData(Core.Scanning.ProjectScanState.Idle, false, "Idle")]
    [InlineData(Core.Scanning.ProjectScanState.Idle, true, "Watching")]
    [InlineData(Core.Scanning.ProjectScanState.Scanning, false, "Scanning")]
    [InlineData(Core.Scanning.ProjectScanState.Error, false, "Error")]
    public void WatcherStateLabel_ScannerStatus_MatchesState(
        Core.Scanning.ProjectScanState state,
        bool isWatching,
        string expectedLabel)
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();

        fixture.Scanner.Raise(
            scanner => scanner.StatusChanged += null,
            fixture.Scanner.Object,
            new Core.Scanning.ProjectScanStatus(
                state,
                isWatching,
                state == Core.Scanning.ProjectScanState.Scanning,
                DateTimeOffset.UtcNow,
                0,
                0,
                state == Core.Scanning.ProjectScanState.Error ? "Scan failed" : null));

        Assert.Equal(expectedLabel, fixture.ViewModel.Scanning.WatcherStateLabel);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, "· 1 folder")]
    [InlineData(412, "· 412 folders")]
    public void WatcherDetail_AppendsExaminedFolderCount(int count, string? expectedEnding)
    {
        var fixture = new SettingsViewModelFixture();
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

        Assert.Equal(count, fixture.ViewModel.Scanning.ScannedDirectoryCount);

        if (expectedEnding is null)
        {
            Assert.StartsWith("Last scan", fixture.ViewModel.Scanning.WatcherDetailText);
            Assert.DoesNotContain("·", fixture.ViewModel.Scanning.WatcherDetailText);
            return;
        }

        Assert.EndsWith(expectedEnding, fixture.ViewModel.Scanning.WatcherDetailText);
    }

    [Fact]
    public void ScanNowCommand_Execute_ReloadsScanner()
    {
        var fixture = new SettingsViewModelFixture();

        fixture.ViewModel.Scanning.ScanNowCommand.Execute(null);

        fixture.Scanner.Verify(scanner => scanner.Reload(), Times.Once);
    }

    [Fact]
    public void ScanWatchedDirectoryCommand_Execute_RescansThatRootOnly()
    {
        var fixture = new SettingsViewModelFixture();
        var directory = @"C:\repos\one";

        fixture.ViewModel.Scanning.ScanWatchedDirectoryCommand.Execute(directory);

        fixture.Scanner.Verify(scanner => scanner.Rescan(directory), Times.Once);
        fixture.Scanner.Verify(scanner => scanner.Reload(), Times.Never);
    }

    [Fact]
    public void AddWatchedDirectoryFromPicker_SelectedFolder_AddsRoot()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();
        var path = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        fixture.Dialogs.Setup(dialogs => dialogs.SelectFolder(It.IsAny<string>())).Returns(path);

        fixture.ViewModel.Scanning.AddWatchedDirectoryFromPickerCommand.Execute(null);

        Assert.Contains(fixture.ViewModel.Scanning.WatchedDirectories, row => string.Equals(row.Path, path, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Save_ValidDraft_PersistsAndRequestsClose()
    {
        var fixture = new SettingsViewModelFixture();
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
        var fixture = new SettingsViewModelFixture();
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
        fixture.ViewModel.Scanning.NewWatchedDirectoryText = path;
        fixture.ViewModel.Scanning.AddWatchedDirectoryCommand.Execute(null);
        var row = fixture.ViewModel.Scanning.WatchedDirectories.Single(candidate => candidate.Path == path);
        row.SelectedDepth = fixture.ViewModel.Scanning.ScanDepthOptions.First(option => option.Value == 4);
        row.SelectedDiscovery = fixture.ViewModel.Scanning.DiscoveryModes.First(option => option.Mode == ProjectDiscoveryMode.Git);

        fixture.ViewModel.SaveCommand.Execute(null);

        Assert.NotNull(saved);
        Assert.True(saved!.Scanning.WatchedDirectorySettings.TryGetValue(path, out var settings));
        Assert.Equal(4, settings!.ScanDepth);
        Assert.Equal(ProjectDiscoveryMode.Git, settings.DiscoveryMode);
    }

    [Fact]
    public void ShowReleaseNotesCommand_Execute_OpensReleaseNotes()
    {
        var fixture = new SettingsViewModelFixture();

        fixture.ViewModel.ShowReleaseNotesCommand.Execute(null);

        fixture.ReleaseNotes.Verify(x => x.ShowReleaseNotes(), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_UpToDate_SetsCurrentStatus()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.UpToDate, "1.0.0", null));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("CLIHub is up to date", fixture.ViewModel.UpdateCard.Title);
        Assert.Equal("Up to date", fixture.ViewModel.UpdateCard.Badge);
        Assert.Equal(SettingBadgeTone.Success, fixture.ViewModel.UpdateCard.Tone);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_UpdateAvailable_IncludesAvailableVersion()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.UpdateAvailable, "1.0.0", "1.1.0"));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("Update available", fixture.ViewModel.UpdateCard.Title);
        Assert.Equal("Update available", fixture.ViewModel.UpdateCard.Badge);
        Assert.Equal(SettingBadgeTone.Accent, fixture.ViewModel.UpdateCard.Tone);
        Assert.Contains("1.1.0", fixture.ViewModel.UpdateCard.Detail);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_NotInstalled_SetsUnavailableStatus()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.NotInstalled, "1.0.0", null));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("Updates unavailable", fixture.ViewModel.UpdateCard.Title);
        Assert.Equal("Not installed", fixture.ViewModel.UpdateCard.Badge);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_Failed_SetsFailureStatus()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.Updates
            .Setup(x => x.CheckForUpdatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateCheckResult(UpdateStatus.Failed, "1.0.0", null));

        await fixture.ViewModel.CheckForUpdatesAsync();

        Assert.Equal("Update check failed", fixture.ViewModel.UpdateCard.Title);
        Assert.Equal("Failed", fixture.ViewModel.UpdateCard.Badge);
    }

    [Fact]
    public void Load_EmptyProbeFields_ShowsBuiltInDefaults()
    {
        var fixture = new SettingsViewModelFixture();

        fixture.ViewModel.Load();

        Assert.Equal("15 min · 10 s", fixture.ViewModel.ProbePolicySummary);
        Assert.Equal("Built-in defaults", fixture.ViewModel.ProbePolicyDetail);
    }

    [Fact]
    public void ProbeFields_PositiveValues_ShowCustomEffectivePolicy()
    {
        var fixture = new SettingsViewModelFixture();
        fixture.ViewModel.Load();

        fixture.ViewModel.ProbeTtlText = "30";
        fixture.ViewModel.ProbeTimeoutText = "5";

        Assert.Equal("30 min · 5 s", fixture.ViewModel.ProbePolicySummary);
        Assert.Equal("Custom detection settings", fixture.ViewModel.ProbePolicyDetail);
    }

    [Fact]
    public void Save_InvalidDraft_DoesNotPersistOrClose()
    {
        var fixture = new SettingsViewModelFixture();
        var closeRequested = false;
        fixture.ViewModel.RequestClose += (_, _) => closeRequested = true;
        fixture.ViewModel.Load();
        fixture.ViewModel.ProbeTimeoutText = "0";

        fixture.ViewModel.SaveCommand.Execute(null);

        Assert.False(closeRequested);
        Assert.Equal("Version check timeout must be a positive number of seconds, or empty for the default.", fixture.ViewModel.ValidationError);
        fixture.Preferences.Verify(x => x.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
    }

    [Fact]
    public void AgentSummary_DetectUpdatesInstalledCount_RaisesPropertyChanged()
    {
        var agents = new Mock<IAgentManagementSection>();
        agents.SetupGet(section => section.InstalledAgentCount).Returns(2);
        agents.SetupGet(section => section.SupportedAgentCount).Returns(5);
        var fixture = new SettingsViewModelFixture(agentsState: agents);
        var raised = new List<string?>();
        fixture.ViewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        agents.Raise(
            section => section.PropertyChanged += null,
            new PropertyChangedEventArgs(nameof(IAgentManagementSection.InstalledAgentCount)));
        agents.Raise(
            section => section.PropertyChanged += null,
            new PropertyChangedEventArgs(nameof(IAgentManagementSection.SupportedAgentCount)));

        Assert.Equal(2, fixture.ViewModel.AgentsInstalledCount);
        Assert.Equal(5, fixture.ViewModel.AgentsSupportedCount);
        Assert.Equal("5 supported", fixture.ViewModel.AgentsSupportSummary);
        Assert.Contains(nameof(SettingsViewModel.AgentsInstalledCount), raised);
        Assert.Contains(nameof(SettingsViewModel.AgentsSupportedCount), raised);
        Assert.Contains(nameof(SettingsViewModel.AgentsSupportSummary), raised);
    }

    private static WatchedDirectoryRow CreateRow(SettingsViewModelFixture fixture, string path) =>
        new(path,
            fixture.ViewModel.Scanning.ScanDepthOptions,
            fixture.ViewModel.Scanning.DiscoveryModes,
            fixture.ViewModel.Scanning.SelectedScanDepth,
            fixture.ViewModel.Scanning.SelectedDiscoveryMode);

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "clihub-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
