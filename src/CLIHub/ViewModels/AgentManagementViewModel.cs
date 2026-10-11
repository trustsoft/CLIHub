namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;

/// <summary>
///   Owns the CLI Agents management surface: one card per agent, the supported/installed metrics,
///   the default-agent selection, and the per-card actions.
/// </summary>
public sealed class AgentManagementViewModel : ObservableObject, IAgentManagementSection
{
    private static readonly AgentOption NoneOption = new(null, "None");

    private readonly IPluginCatalog _catalog;
    private readonly IAgentDetectionService _detection;
    private readonly IAgentVersionService _version;
    private readonly IAgentManagementService _management;
    private readonly IExternalLauncher _launcher;
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly ILogger<AgentManagementViewModel> _logger;

    private string _metrics = "0 supported · 0 installed";
    private IReadOnlyList<AgentOption> _defaultAgentOptions = [NoneOption];
    private AgentOption _selectedDefaultAgent = NoneOption;
    private bool _disposed;

    /// <summary>
    ///   Creates the section.
    /// </summary>
    /// <param name="catalog"> Plugin catalog supplying the agents. </param>
    /// <param name="detection"> Host availability detection. </param>
    /// <param name="version"> Version probe service. </param>
    /// <param name="management"> On-demand probe/update operations. </param>
    /// <param name="launcher"> Opens the agent's configuration file. </param>
    /// <param name="operationLifetime"> Application lifetime for tracked actions. </param>
    /// <param name="logger"> Logger. </param>
    public AgentManagementViewModel(
        IPluginCatalog catalog,
        IAgentDetectionService detection,
        IAgentVersionService version,
        IAgentManagementService management,
        IExternalLauncher launcher,
        IApplicationOperationLifetime operationLifetime,
        ILogger<AgentManagementViewModel> logger)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _detection = detection ?? throw new ArgumentNullException(nameof(detection));
        _version = version ?? throw new ArgumentNullException(nameof(version));
        _management = management ?? throw new ArgumentNullException(nameof(management));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        TestCommand = new RelayCommand<AgentCardItem>(card => _ = RunAsync("Agent test", ct => TestAsync(card, ct)));
        UpdateCommand = new RelayCommand<AgentCardItem>(card => _ = RunAsync("Agent update", ct => UpdateAsync(card, ct)));
        EditConfigCommand = new RelayCommand<AgentCardItem>(EditConfig);
    }

    /// <inheritdoc />
    public ObservableCollection<AgentCardItem> Cards { get; } = new();

    /// <inheritdoc />
    public string Metrics
    {
        get => _metrics;
        private set => SetProperty(ref _metrics, value);
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentOption> DefaultAgentOptions
    {
        get => _defaultAgentOptions;
        private set => SetProperty(ref _defaultAgentOptions, value);
    }

    /// <inheritdoc />
    public AgentOption SelectedDefaultAgent
    {
        get => _selectedDefaultAgent;
        set => SetProperty(ref _selectedDefaultAgent, value ?? NoneOption);
    }

    /// <inheritdoc />
    public string? CurrentDefaultAgentId => SelectedDefaultAgent.Id;

    /// <inheritdoc />
    public RelayCommand<AgentCardItem> TestCommand { get; }

    /// <inheritdoc />
    public RelayCommand<AgentCardItem> UpdateCommand { get; }

    /// <inheritdoc />
    public RelayCommand<AgentCardItem> EditConfigCommand { get; }

    /// <inheritdoc />
    public void Load(string? defaultAgentId)
    {
        if (_disposed)
        {
            return;
        }

        Cards.Clear();

        foreach (var plugin in _catalog.GetAllPlugins())
        {
            var isInstalled = _detection.IsInstalledInSystem(plugin);
            var canUpdate = plugin.Commands?.Get(AgentCommandKind.Update) is not null;
            var card = new AgentCardItem(
                plugin,
                plugin.LogoPath,
                isInstalled,
                "…",
                AgentConfigurationPath.For(plugin))
            {
                CanUpdate = canUpdate,
                UpdateNote = canUpdate ? null : "Update not supported"
            };

            Cards.Add(card);
        }

        RefreshMetrics();
        RebuildDefaultAgentOptions(defaultAgentId);
        PopulateVersions();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
    }

    private void RebuildDefaultAgentOptions(string? defaultAgentId)
    {
        var options = new List<AgentOption> { NoneOption };
        options.AddRange(Cards.Select(card => new AgentOption(card.PluginId, card.Name, card.Version)));
        DefaultAgentOptions = options;

        SelectedDefaultAgent = options.FirstOrDefault(o => o.Id == defaultAgentId) ?? NoneOption;
    }

    private void RefreshMetrics()
    {
        var installed = Cards.Count(card => card.IsInstalled);
        Metrics = $"{Cards.Count} supported · {installed} installed";
    }

    private void PopulateVersions() =>
        _ = RunAsync("Agent version population", async cancellationToken =>
        {
            foreach (var card in Cards.ToList())
            {
                var version = await _version.GetVersionAsync(card.Plugin, cancellationToken);
                if (!_disposed)
                {
                    card.Version = version ?? "unknown";
                }
            }

            if (!_disposed)
            {
                RebuildDefaultAgentOptions(SelectedDefaultAgent?.Id);
            }
        });

    private async Task TestAsync(AgentCardItem card, CancellationToken cancellationToken)
    {
        var result = await _management.ProbeAsync(card.Plugin, cancellationToken);
        card.IsInstalled = result.IsInstalled;
        card.Version = result.Version ?? "unknown";
        card.Message = result.IsInstalled
            ? $"Installed · version {card.Version}"
            : "Not installed on the host";
        RefreshMetrics();
    }

    private async Task UpdateAsync(AgentCardItem card, CancellationToken cancellationToken)
    {
        card.Message = $"Updating {card.Name}…";
        var result = await _management.UpdateAsync(card.Plugin, cancellationToken);
        card.Message = result.Outcome switch
        {
            AgentUpdateOutcome.Started => "Update started.",
            _ => result.Message ?? "Update failed."
        };
    }

    private void EditConfig(AgentCardItem card)
    {
        if (card.ConfigPath is not { } path)
        {
            return;
        }

        try
        {
            _launcher.Open(path);
            card.Message = $"Opened {path}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not open agent config {Path}", path);
            card.Message = $"Could not open {path}";
        }
    }

    private Task RunAsync(string operationName, Func<CancellationToken, Task> operation) =>
        _operationLifetime.RunAsync(operationName, operation);
}
