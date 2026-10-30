namespace CLIHub.Tests.Builders;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Processes;
using CLIHub.ViewModels;
using CLIHub.Lifetime;
using CLIHub.Launching;

using static CLIHub.Tests.Support.TestHelpers;

/// <summary>
///   Fluent builder for AgentManagementViewModel test instances with sensible defaults.
/// </summary>
public class AgentManagementViewModelBuilder
{
    private Mock<IPluginCatalog>? pluginCatalog;
    private Mock<IAgentDetectionService>? detection;
    private Mock<IAgentVersionService>? versions;
    private Mock<IAgentUpdateAvailabilityService>? availability;
    private Mock<IAgentProbeService>? probe;
    private Mock<IAgentUpdateService>? update;
    private Mock<IExternalLauncher>? launcher;
    private Mock<IAgentProcessInspector>? processInspector;
    private IApplicationOperationLifetime? operationLifetime;
    private ILogger<AgentManagementViewModel>? logger;
    private ILogger<AgentProcessMonitor>? processMonitorLogger;

    private Plugin[]? plugins;
    private InvalidPluginEntry[]? invalidPlugins;

    /// <summary>
    ///   Configures the plugin catalog with specific plugins.
    /// </summary>
    public AgentManagementViewModelBuilder WithPlugins(params Plugin[] plugins)
    {
        this.plugins = plugins;
        return this;
    }

    /// <summary>
    ///   Configures the invalid plugin entries.
    /// </summary>
    public AgentManagementViewModelBuilder WithInvalidPlugins(params InvalidPluginEntry[] invalidPlugins)
    {
        this.invalidPlugins = invalidPlugins;
        return this;
    }

    /// <summary>
    ///   Provides a custom plugin catalog mock.
    /// </summary>
    public AgentManagementViewModelBuilder WithPluginCatalog(Mock<IPluginCatalog> pluginCatalog)
    {
        this.pluginCatalog = pluginCatalog;
        return this;
    }

    /// <summary>
    ///   Provides a custom agent detection service mock.
    /// </summary>
    public AgentManagementViewModelBuilder WithDetection(Mock<IAgentDetectionService> detection)
    {
        this.detection = detection;
        return this;
    }

    /// <summary>
    ///   Provides a custom agent version service mock.
    /// </summary>
    public AgentManagementViewModelBuilder WithVersions(Mock<IAgentVersionService> versions)
    {
        this.versions = versions;
        return this;
    }

    /// <summary>
    ///   Provides a custom agent update availability service mock.
    /// </summary>
    public AgentManagementViewModelBuilder WithAvailability(Mock<IAgentUpdateAvailabilityService> availability)
    {
        this.availability = availability;
        return this;
    }

    /// <summary>
    ///   Provides a custom agent probe service mock.
    /// </summary>
    public AgentManagementViewModelBuilder WithProbe(Mock<IAgentProbeService> probe)
    {
        this.probe = probe;
        return this;
    }

    /// <summary>
    ///   Provides a custom agent update service mock.
    /// </summary>
    public AgentManagementViewModelBuilder WithUpdate(Mock<IAgentUpdateService> update)
    {
        this.update = update;
        return this;
    }

    /// <summary>
    ///   Provides a custom external launcher mock.
    /// </summary>
    public AgentManagementViewModelBuilder WithLauncher(Mock<IExternalLauncher> launcher)
    {
        this.launcher = launcher;
        return this;
    }

    /// <summary>
    ///   Provides a custom agent process inspector mock.
    /// </summary>
    public AgentManagementViewModelBuilder WithProcessInspector(Mock<IAgentProcessInspector> processInspector)
    {
        this.processInspector = processInspector;
        return this;
    }

    /// <summary>
    ///   Provides a custom application operation lifetime.
    /// </summary>
    public AgentManagementViewModelBuilder WithOperationLifetime(IApplicationOperationLifetime operationLifetime)
    {
        this.operationLifetime = operationLifetime;
        return this;
    }

    /// <summary>
    ///   Provides a custom logger for the view model.
    /// </summary>
    public AgentManagementViewModelBuilder WithLogger(ILogger<AgentManagementViewModel> logger)
    {
        this.logger = logger;
        return this;
    }

    /// <summary>
    ///   Provides a custom logger for the process monitor.
    /// </summary>
    public AgentManagementViewModelBuilder WithProcessMonitorLogger(ILogger<AgentProcessMonitor> processMonitorLogger)
    {
        this.processMonitorLogger = processMonitorLogger;
        return this;
    }

    /// <summary>
    ///   Builds an AgentManagementViewModel instance with all dependencies configured.
    /// </summary>
    public AgentManagementViewModel Build()
    {
        var (viewModel, _) = this.Compose();
        return viewModel;
    }

    /// <summary>
    ///   Builds the ViewModel and returns it along with all mock services for verification.
    /// </summary>
    public (AgentManagementViewModel ViewModel, AgentMocks Mocks) BuildWithMocks() => this.Compose();

    /// <summary>
    ///   Constructs the view model and its mock services once for both public build methods.
    /// </summary>
    private (AgentManagementViewModel ViewModel, AgentMocks Mocks) Compose()
    {
        var catalog = this.pluginCatalog ?? this.CreateDefaultPluginCatalog();
        var detection = this.detection ?? new Mock<IAgentDetectionService>();
        var versions = this.versions ?? new Mock<IAgentVersionService>();
        var availability = this.availability ?? new Mock<IAgentUpdateAvailabilityService>();
        var probe = this.probe ?? new Mock<IAgentProbeService>();
        var update = this.update ?? new Mock<IAgentUpdateService>();
        var launcher = this.launcher ?? new Mock<IExternalLauncher>();
        var processInspector = this.processInspector ?? this.CreateDefaultProcessInspector();

        var processMonitor = new AgentProcessMonitor(
            new ProcessOrchestrator(
                Mock.Of<IRuntimeResolver>(),
                Mock.Of<ICommandLineBuilder>(),
                Mock.Of<IInteractiveProcessRunner>(),
                Mock.Of<IProcessOutputRunner>(),
                new AgentProcessFinder(processInspector.Object, new AgentProcessMatcher()),
                new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance),
                NullLogger<ProcessOrchestrator>.Instance),
            catalog.Object,
            new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance),
            this.processMonitorLogger ?? NullLogger<AgentProcessMonitor>.Instance);

        var viewModel = new AgentManagementViewModel(
            catalog.Object,
            detection.Object,
            versions.Object,
            availability.Object,
            probe.Object,
            update.Object,
            launcher.Object,
            processMonitor,
            this.operationLifetime ?? new InlineOperationLifetime(),
            this.logger ?? NullLogger<AgentManagementViewModel>.Instance);

        var mocks = new AgentMocks
        {
            PluginCatalog = catalog,
            Detection = detection,
            Versions = versions,
            Availability = availability,
            Probe = probe,
            Update = update,
            Launcher = launcher,
            ProcessInspector = processInspector
        };

        return (viewModel, mocks);
    }

    private Mock<IPluginCatalog> CreateDefaultPluginCatalog()
    {
        var mock = new Mock<IPluginCatalog>();
        mock.Setup(x => x.GetAllPlugins()).Returns(this.plugins ?? []);
        mock.Setup(x => x.GetInvalidPlugins()).Returns(this.invalidPlugins ?? []);
        return mock;
    }

    private Mock<IAgentProcessInspector> CreateDefaultProcessInspector()
    {
        var mock = new Mock<IAgentProcessInspector>();
        mock.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([]);
        return mock;
    }

    /// <summary>
    ///   Container for all mock services created during test setup.
    /// </summary>
    public class AgentMocks
    {
        public required Mock<IPluginCatalog> PluginCatalog { get; init; }
        public required Mock<IAgentDetectionService> Detection { get; init; }
        public required Mock<IAgentVersionService> Versions { get; init; }
        public required Mock<IAgentUpdateAvailabilityService> Availability { get; init; }
        public required Mock<IAgentProbeService> Probe { get; init; }
        public required Mock<IAgentUpdateService> Update { get; init; }
        public required Mock<IExternalLauncher> Launcher { get; init; }
        public required Mock<IAgentProcessInspector> ProcessInspector { get; init; }
    }
}
