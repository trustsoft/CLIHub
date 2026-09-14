using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed class AgentDetector
{
    private readonly ConfigStore _store;
    private readonly IFileSystem _fileSystem;
    private readonly IProcessRunner _processRunner;
    private readonly IClock _clock;
    private readonly object _gate = new();

    private Dictionary<string, AgentHostStatus> _hostStatus = new();

    public AgentDetector(ConfigStore store, IFileSystem fileSystem, IProcessRunner processRunner, IClock clock)
    {
        _store = store;
        _fileSystem = fileSystem;
        _processRunner = processRunner;
        _clock = clock;
        LoadFromConfig();
    }

    public event Action? RoundCompleted;

    public AgentHostStatus GetHostStatus(AgentManifest agent)
    {
        lock (_gate)
        {
            return _hostStatus.TryGetValue(agent.Id ?? string.Empty, out var status)
                ? status
                : AgentHostStatus.NotInstalled;
        }
    }

    public bool IsProjectInitialized(AgentManifest agent, string projectPath)
    {
        var paths = agent.Detect?.Project;
        if (paths is null || paths.Count == 0)
        {
            return true;
        }

        foreach (var relative in paths)
        {
            if (string.IsNullOrWhiteSpace(relative))
            {
                continue;
            }

            var full = Path.Combine(projectPath, relative);
            if (_fileSystem.FileExists(full) || _fileSystem.DirectoryExists(full))
            {
                return true;
            }
        }

        return false;
    }

    public ActionAvailability GetAvailability(AgentManifest agent, string? projectPath)
    {
        var host = GetHostStatus(agent);
        var initialized = projectPath is not null && IsProjectInitialized(agent, projectPath);
        var derived = DeriveAvailability(host.HostInstalled, initialized);

        bool Has(string actionKey) =>
            agent.Actions is not null &&
            agent.Actions.TryGetValue(actionKey, out var action) &&
            !string.IsNullOrWhiteSpace(action.Command);

        return new ActionAvailability(
            Run: derived.Run && Has("run"),
            Resume: derived.Resume && Has("resume"),
            Init: derived.Init && Has("init"),
            Update: derived.Update && Has("update"));
    }

    public static ActionAvailability DeriveAvailability(bool hostInstalled, bool projectInitialized) => new(
        Run: hostInstalled && projectInitialized,
        Resume: hostInstalled && projectInitialized,
        Init: hostInstalled && !projectInitialized,
        Update: hostInstalled);

    public void LoadFromConfig()
    {
        var config = _store.Load();
        lock (_gate)
        {
            _hostStatus = ToStatusMap(config);
        }
    }

    public void RunStartupRound(IReadOnlyList<AgentManifest> agents)
    {
        var config = _store.Load();
        var ttl = TimeSpan.FromMinutes(config.Probe.EffectiveTtlMinutes);
        var timeoutSeconds = config.Probe.EffectiveTimeoutSeconds;
        var probed = new Dictionary<string, AgentProbeEntry>();

        foreach (var agent in agents)
        {
            var id = agent.Id ?? string.Empty;
            if (config.Agents.TryGetValue(id, out var existing) && IsFresh(existing, ttl))
            {
                continue;
            }

            var status = ProbeHost(agent, timeoutSeconds);
            probed[id] = new AgentProbeEntry
            {
                HostInstalled = status.HostInstalled,
                Version = status.Version,
                LastProbed = _clock.UtcNow
            };
        }

        if (probed.Count > 0)
        {
            var latest = _store.Load();
            foreach (var entry in probed)
            {
                latest.Agents[entry.Key] = entry.Value;
            }

            _store.Save(latest);
        }

        lock (_gate)
        {
            var merged = ToStatusMap(config);
            foreach (var entry in probed)
            {
                merged[entry.Key] = new AgentHostStatus(entry.Value.HostInstalled, entry.Value.Version);
            }

            _hostStatus = merged;
        }

        RoundCompleted?.Invoke();
    }

    private bool IsFresh(AgentProbeEntry entry, TimeSpan ttl) =>
        entry.LastProbed is { } lastProbed && _clock.UtcNow - lastProbed < ttl;

    private AgentHostStatus ProbeHost(AgentManifest agent, int timeoutSeconds)
    {
        var versionCommand = agent.Actions?.GetValueOrDefault("version")?.Command;

        if (string.IsNullOrWhiteSpace(versionCommand))
        {
            var runCommand = agent.Actions?.GetValueOrDefault("run")?.Command;
            if (string.IsNullOrWhiteSpace(runCommand))
            {
                return AgentHostStatus.NotInstalled;
            }

            return new AgentHostStatus(_processRunner.CommandExists(ExecutableOf(runCommand)), null);
        }

        var result = _processRunner.RunProbe(
            ExecutableOf(versionCommand),
            ArgumentsOf(versionCommand),
            timeoutSeconds);

        if (result.TimedOut || result.ExitCode != 0)
        {
            return AgentHostStatus.NotInstalled;
        }

        return new AgentHostStatus(true, FirstLine(result.Output));
    }

    private Dictionary<string, AgentHostStatus> ToStatusMap(Config config) =>
        config.Agents.ToDictionary(
            entry => entry.Key,
            entry => new AgentHostStatus(entry.Value.HostInstalled, entry.Value.Version));

    private static string ExecutableOf(string command) => command.Split(' ', 2)[0];

    private static string ArgumentsOf(string command) =>
        command.Contains(' ') ? command.Split(' ', 2)[1] : string.Empty;

    private static string? FirstLine(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        return output.Split('\n')[0].Trim();
    }
}
