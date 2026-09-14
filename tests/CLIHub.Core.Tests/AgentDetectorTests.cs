using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class AgentDetectorTests
{
    private readonly CapturingProcessRunner _runner = new();
    private readonly StubClock _clock = new();

    private AgentDetector CreateDetector(string configDirectory) =>
        new(
            new ConfigStore(new PhysicalFileSystem(), new StubPathProvider(configDirectory, configDirectory)),
            new PhysicalFileSystem(),
            _runner,
            _clock);

    private static AgentManifest Manifest(
        string id = "claude",
        string? versionCommand = "claude --version",
        string runCommand = "claude",
        string? detectProject = null)
    {
        var actions = new Dictionary<string, AgentActionSpec>
        {
            ["run"] = new() { Command = runCommand },
            ["resume"] = new() { Command = $"{runCommand} --continue" },
            ["init"] = new() { Command = $"{runCommand} init" },
            ["update"] = new() { Command = $"{runCommand} update" }
        };

        if (versionCommand is not null)
        {
            actions["version"] = new() { Command = versionCommand };
        }

        return new AgentManifest
        {
            SchemaVersion = 1,
            Id = id,
            Name = id,
            Actions = actions,
            Detect = detectProject is null
                ? null
                : new DetectSpec { Project = detectProject.Split(',').ToList() }
        };
    }

    // 2.1 Host probe

    [Fact]
    public void Probe_ExitZero_InstallsWithFirstLineVersion()
    {
        using var temp = new TempDirectory();
        _runner.ProbeHandler = (_, _, _) => new ProbeResult(0, "1.2.3\r\nextra line\r\n", false);
        var detector = CreateDetector(temp.Path);

        detector.RunStartupRound(new[] { Manifest() });

        var status = detector.GetHostStatus(Manifest());
        Assert.True(status.HostInstalled);
        Assert.Equal("1.2.3", status.Version);
    }

    [Fact]
    public void Probe_PaddedVersion_IsTrimmed()
    {
        using var temp = new TempDirectory();
        _runner.ProbeHandler = (_, _, _) => new ProbeResult(0, "  2.0.1  \nsecond", false);
        var detector = CreateDetector(temp.Path);

        detector.RunStartupRound(new[] { Manifest() });

        Assert.Equal("2.0.1", detector.GetHostStatus(Manifest()).Version);
    }

    [Fact]
    public void Probe_NonZeroExit_NotInstalled()
    {
        using var temp = new TempDirectory();
        _runner.ProbeHandler = (_, _, _) => new ProbeResult(1, "boom", false);
        var detector = CreateDetector(temp.Path);

        detector.RunStartupRound(new[] { Manifest() });

        var status = detector.GetHostStatus(Manifest());
        Assert.False(status.HostInstalled);
        Assert.Null(status.Version);
    }

    [Fact]
    public void Probe_Timeout_NotInstalled()
    {
        using var temp = new TempDirectory();
        _runner.ProbeHandler = (_, _, _) => new ProbeResult(-1, null, TimedOut: true);
        var detector = CreateDetector(temp.Path);

        detector.RunStartupRound(new[] { Manifest() });

        Assert.False(detector.GetHostStatus(Manifest()).HostInstalled);
    }

    [Fact]
    public void Probe_SplitsExecutableAndArguments()
    {
        using var temp = new TempDirectory();
        _runner.ProbeHandler = (_, _, _) => new ProbeResult(0, "1.0.0", false);
        var detector = CreateDetector(temp.Path);

        detector.RunStartupRound(new[] { Manifest() });

        var probe = Assert.Single(_runner.Probes);
        Assert.Equal("claude", probe.FileName);
        Assert.Equal("--version", probe.Arguments);
        Assert.Equal(ProbeConfig.DefaultTimeoutSeconds, probe.TimeoutSeconds);
    }

    // 2.2 PATH fallback

    [Fact]
    public void NoVersionAction_RunExecutableOnPath_InstalledWithoutVersion()
    {
        using var temp = new TempDirectory();
        _runner.CommandExistsHandler = command => command == "pi";
        var detector = CreateDetector(temp.Path);

        detector.RunStartupRound(new[] { Manifest(id: "pi", versionCommand: null, runCommand: "pi") });

        var status = detector.GetHostStatus(Manifest(id: "pi", versionCommand: null, runCommand: "pi"));
        Assert.True(status.HostInstalled);
        Assert.Null(status.Version);
        Assert.Empty(_runner.Probes);
    }

    [Fact]
    public void NoVersionAction_RunExecutableMissing_NotInstalled()
    {
        using var temp = new TempDirectory();
        _runner.CommandExistsHandler = _ => false;
        var detector = CreateDetector(temp.Path);

        detector.RunStartupRound(new[] { Manifest(versionCommand: null) });

        Assert.False(detector.GetHostStatus(Manifest(versionCommand: null)).HostInstalled);
    }

    // 2.3 Startup round: TTL cache, single write

    [Fact]
    public void Round_FreshCacheEntry_SkipsProbe()
    {
        using var temp = new TempDirectory();
        SeedAgents(temp.Path, new AgentProbeEntry
        {
            HostInstalled = true,
            Version = "cached",
            LastProbed = _clock.UtcNow
        });
        var detector = CreateDetector(temp.Path);

        detector.RunStartupRound(new[] { Manifest() });

        Assert.Empty(_runner.Probes);
        Assert.Equal("cached", detector.GetHostStatus(Manifest()).Version);
    }

    [Fact]
    public void Round_StaleCacheEntry_ProbesAgainAndUpdates()
    {
        using var temp = new TempDirectory();
        SeedAgents(temp.Path, new AgentProbeEntry
        {
            HostInstalled = false,
            Version = null,
            LastProbed = _clock.UtcNow - TimeSpan.FromMinutes(ProbeConfig.DefaultTtlMinutes + 1)
        });
        _runner.ProbeHandler = (_, _, _) => new ProbeResult(0, "9.9.9", false);
        var detector = CreateDetector(temp.Path);

        detector.RunStartupRound(new[] { Manifest() });

        var status = detector.GetHostStatus(Manifest());
        Assert.True(status.HostInstalled);
        Assert.Equal("9.9.9", status.Version);

        var config = LoadConfig(temp.Path);
        var entry = config.Agents["claude"];
        Assert.True(entry.HostInstalled);
        Assert.Equal("9.9.9", entry.Version);
        Assert.Equal(_clock.UtcNow, entry.LastProbed);
    }

    [Fact]
    public void Round_WritesAllAgentsAndPreservesOtherSections()
    {
        using var temp = new TempDirectory();
        var store = new ConfigStore(
            new PhysicalFileSystem(),
            new StubPathProvider(temp.Path, temp.Path));
        store.Save(new Config { Hotkey = "Ctrl+Shift+K" });
        _runner.ProbeHandler = (_, _, _) => new ProbeResult(0, "1.0.0", false);
        var detector = CreateDetector(temp.Path);

        detector.RunStartupRound(new[] { Manifest(id: "claude"), Manifest(id: "opencode", versionCommand: "opencode --version", runCommand: "opencode") });

        var config = LoadConfig(temp.Path);
        Assert.Equal(2, config.Agents.Count);
        Assert.True(config.Agents["claude"].HostInstalled);
        Assert.True(config.Agents["opencode"].HostInstalled);
        Assert.Equal("Ctrl+Shift+K", config.Hotkey);
    }

    [Fact]
    public void Round_AllEntriesFresh_DoesNotRewriteConfig()
    {
        using var temp = new TempDirectory();
        SeedAgents(temp.Path, new AgentProbeEntry
        {
            HostInstalled = true,
            Version = "cached",
            LastProbed = _clock.UtcNow
        });
        var before = File.ReadAllText(Path.Combine(temp.Path, ConfigStore.FileName));
        var detector = CreateDetector(temp.Path);

        detector.RunStartupRound(new[] { Manifest() });

        Assert.Equal(before, File.ReadAllText(Path.Combine(temp.Path, ConfigStore.FileName)));
    }

    [Fact]
    public void Round_RaisesRoundCompleted()
    {
        using var temp = new TempDirectory();
        var detector = CreateDetector(temp.Path);
        var raised = false;
        detector.RoundCompleted += () => raised = true;

        detector.RunStartupRound(new[] { Manifest() });

        Assert.True(raised);
    }

    // 2.4 Project detection

    [Fact]
    public void Project_MarkerFilePresent_Initialized()
    {
        using var temp = new TempDirectory();
        var project = Path.Combine(temp.Path, "project");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, "CLAUDE.md"), "# notes");
        var detector = CreateDetector(temp.Path);

        Assert.True(detector.IsProjectInitialized(Manifest(detectProject: ".claude,CLAUDE.md"), project));
    }

    [Fact]
    public void Project_MarkerDirectoryPresent_Initialized()
    {
        using var temp = new TempDirectory();
        var project = Path.Combine(temp.Path, "project");
        var marker = Path.Combine(project, ".claude");
        Directory.CreateDirectory(marker);
        var detector = CreateDetector(temp.Path);

        Assert.True(detector.IsProjectInitialized(Manifest(detectProject: ".claude,CLAUDE.md"), project));
    }

    [Fact]
    public void Project_NoMarkersPresent_NotInitialized()
    {
        using var temp = new TempDirectory();
        var project = Path.Combine(temp.Path, "project");
        Directory.CreateDirectory(project);
        var detector = CreateDetector(temp.Path);

        Assert.False(detector.IsProjectInitialized(Manifest(detectProject: ".claude,CLAUDE.md"), project));
    }

    [Fact]
    public void Project_NoPathsDeclared_AlwaysInitialized()
    {
        using var temp = new TempDirectory();
        var project = Path.Combine(temp.Path, "project");
        Directory.CreateDirectory(project);
        var detector = CreateDetector(temp.Path);

        Assert.True(detector.IsProjectInitialized(Manifest(detectProject: null), project));
        Assert.True(detector.IsProjectInitialized(Manifest(detectProject: null), temp.Path));
    }

    // 2.5 Availability derivation

    [Fact]
    public void Availability_InstalledAndInitialized_RunResumeUpdate()
    {
        var availability = AgentDetector.DeriveAvailability(hostInstalled: true, projectInitialized: true);

        Assert.True(availability.Run);
        Assert.True(availability.Resume);
        Assert.False(availability.Init);
        Assert.True(availability.Update);
    }

    [Fact]
    public void Availability_InstalledNotInitialized_InitUpdate()
    {
        var availability = AgentDetector.DeriveAvailability(hostInstalled: true, projectInitialized: false);

        Assert.False(availability.Run);
        Assert.False(availability.Resume);
        Assert.True(availability.Init);
        Assert.True(availability.Update);
    }

    [Fact]
    public void Availability_NotInstalled_Nothing()
    {
        var availability = AgentDetector.DeriveAvailability(hostInstalled: false, projectInitialized: true);

        Assert.Equal(ActionAvailability.None, availability);
    }

    [Fact]
    public void Availability_RequiresActionInManifest()
    {
        using var temp = new TempDirectory();
        SeedAgents(temp.Path, new AgentProbeEntry { HostInstalled = true, LastProbed = _clock.UtcNow });
        var detector = CreateDetector(temp.Path);
        var manifest = Manifest();
        manifest.Actions.Remove("init");
        var project = Path.Combine(temp.Path, "project");
        Directory.CreateDirectory(project);

        var availability = detector.GetAvailability(manifest, project);

        Assert.True(availability.Run);
        Assert.False(availability.Init);
    }

    [Fact]
    public void Availability_WithoutProject_InitOnly()
    {
        using var temp = new TempDirectory();
        SeedAgents(temp.Path, new AgentProbeEntry { HostInstalled = true, LastProbed = _clock.UtcNow });
        var detector = CreateDetector(temp.Path);

        var availability = detector.GetAvailability(Manifest(), projectPath: null);

        Assert.False(availability.Run);
        Assert.True(availability.Init);
    }

    private void SeedAgents(string configDirectory, AgentProbeEntry entry)
    {
        var store = new ConfigStore(
            new PhysicalFileSystem(),
            new StubPathProvider(configDirectory, configDirectory));
        var config = store.Load();
        config.Agents["claude"] = entry;
        store.Save(config);
    }

    private static Config LoadConfig(string configDirectory) =>
        new ConfigStore(
            new PhysicalFileSystem(),
            new StubPathProvider(configDirectory, configDirectory)).Load();
}
