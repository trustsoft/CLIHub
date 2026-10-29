namespace CLIHub.Tests.Builders;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Processes;
using CLIHub.Core.Projects;
using CLIHub.ViewModels;
using CLIHub.Launching;
using CLIHub.Settings;
using CLIHub.Lifetime;
using CLIHub.Updates;

/// <summary>
///   Fluent builder for LaunchWindowViewModel test instances with sensible defaults.
/// </summary>
public class LaunchWindowViewModelBuilder
{
    private Mock<IProjectService>? projectService;
    private Mock<IPluginCatalog>? pluginCatalog;
    private Mock<IAgentDetectionService>? agentDetectionService;
    private Mock<IAgentVersionService>? agentVersionService;
    private Mock<ILogoCacheService>? logoCacheService;
    private Mock<IPreferencesStore>? preferencesStore;
    private Mock<IProjectDialogService>? projectDialogService;
    private Mock<IAgentCommandWorkflow>? agentCommandWorkflow;
    private Mock<IUpdateWorkflow>? updateWorkflow;
    private Mock<IUserNotificationService>? userNotificationService;
    private Mock<IApplicationLifetime>? applicationLifetime;
    private Mock<IApplicationOperationLifetime>? operationLifetime;
    private Mock<ISettingsLauncher>? settingsLauncher;
    private Mock<IExternalLauncher>? externalLauncher;
    private Mock<IAgentProcessInspector>? processInspector;

    private Project[]? projects;
    private Project? currentProject;
    private Plugin[]? plugins;
    private AppPreferences? preferences;
    private string? currentVersion;

    /// <summary>
    ///   Configures the project service with specific projects.
    /// </summary>
    public LaunchWindowViewModelBuilder WithProjects(params Project[] projects)
    {
        this.projects = projects;
        return this;
    }

    /// <summary>
    ///   Configures the current project selection.
    /// </summary>
    public LaunchWindowViewModelBuilder WithCurrentProject(Project? project)
    {
        this.currentProject = project;
        return this;
    }

    /// <summary>
    ///   Configures the plugin catalog with specific plugins.
    /// </summary>
    public LaunchWindowViewModelBuilder WithPlugins(params Plugin[] plugins)
    {
        this.plugins = plugins;
        return this;
    }

    /// <summary>
    ///   Configures application preferences.
    /// </summary>
    public LaunchWindowViewModelBuilder WithPreferences(AppPreferences preferences)
    {
        this.preferences = preferences;
        return this;
    }

    /// <summary>
    ///   Provides a custom project service mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithProjectService(Mock<IProjectService> projectService)
    {
        this.projectService = projectService;
        return this;
    }

    /// <summary>
    ///   Provides a custom plugin catalog mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithPluginCatalog(Mock<IPluginCatalog> pluginCatalog)
    {
        this.pluginCatalog = pluginCatalog;
        return this;
    }

    /// <summary>
    ///   Provides a custom agent detection service mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithAgentDetectionService(Mock<IAgentDetectionService> detection)
    {
        this.agentDetectionService = detection;
        return this;
    }

    /// <summary>
    ///   Provides a custom project dialog service mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithProjectDialogService(Mock<IProjectDialogService> dialogService)
    {
        this.projectDialogService = dialogService;
        return this;
    }

    /// <summary>
    ///   Provides a custom user notification service mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithUserNotificationService(Mock<IUserNotificationService> notificationService)
    {
        this.userNotificationService = notificationService;
        return this;
    }

    /// <summary>
    ///   Provides a custom application lifetime mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithApplicationLifetime(Mock<IApplicationLifetime> lifetime)
    {
        this.applicationLifetime = lifetime;
        return this;
    }

    /// <summary>
    ///   Provides a custom update workflow mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithUpdateWorkflow(Mock<IUpdateWorkflow> updateWorkflow)
    {
        this.updateWorkflow = updateWorkflow;
        return this;
    }

    /// <summary>
    ///   Provides a custom agent version service mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithAgentVersionService(Mock<IAgentVersionService> agentVersionService)
    {
        this.agentVersionService = agentVersionService;
        return this;
    }

    /// <summary>
    ///   Provides a custom logo cache service mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithLogoCacheService(Mock<ILogoCacheService> logoCacheService)
    {
        this.logoCacheService = logoCacheService;
        return this;
    }

    /// <summary>
    ///   Provides a custom preferences store mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithPreferencesStore(Mock<IPreferencesStore> preferencesStore)
    {
        this.preferencesStore = preferencesStore;
        return this;
    }

    /// <summary>
    ///   Provides a custom agent command workflow mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithAgentCommandWorkflow(Mock<IAgentCommandWorkflow> agentCommandWorkflow)
    {
        this.agentCommandWorkflow = agentCommandWorkflow;
        return this;
    }

    /// <summary>
    ///   Provides a custom application operation lifetime mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithOperationLifetime(Mock<IApplicationOperationLifetime> operationLifetime)
    {
        this.operationLifetime = operationLifetime;
        return this;
    }

    /// <summary>
    ///   Provides a custom settings launcher mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithSettingsLauncher(Mock<ISettingsLauncher> settingsLauncher)
    {
        this.settingsLauncher = settingsLauncher;
        return this;
    }

    /// <summary>
    ///   Provides a custom external launcher mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithExternalLauncher(Mock<IExternalLauncher> externalLauncher)
    {
        this.externalLauncher = externalLauncher;
        return this;
    }

    /// <summary>
    ///   Provides a custom agent process inspector mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithProcessInspector(Mock<IAgentProcessInspector> processInspector)
    {
        this.processInspector = processInspector;
        return this;
    }

    /// <summary>
    ///   Configures the version reported by the default update workflow.
    /// </summary>
    public LaunchWindowViewModelBuilder WithCurrentVersion(string currentVersion)
    {
        this.currentVersion = currentVersion;
        return this;
    }

    /// <summary>
    ///   Builds a LaunchWindowViewModel instance with all dependencies configured.
    /// </summary>
    public LaunchWindowViewModel Build()
    {
        var projectService = this.projectService ?? this.CreateDefaultProjectService();
        var pluginCatalog = this.pluginCatalog ?? this.CreateDefaultPluginCatalog();
        var agentDetectionService = this.agentDetectionService ?? new Mock<IAgentDetectionService>();
        var agentVersionService = this.agentVersionService ?? new Mock<IAgentVersionService>();
        var logoCacheService = this.logoCacheService ?? new Mock<ILogoCacheService>();
        var preferencesStore = this.preferencesStore ?? this.CreateDefaultPreferencesStore();
        var projectDialogService = this.projectDialogService ?? new Mock<IProjectDialogService>();
        var agentCommandWorkflow = this.agentCommandWorkflow ?? new Mock<IAgentCommandWorkflow>();
        var updateWorkflow = this.updateWorkflow ?? this.CreateDefaultUpdateWorkflow();
        var userNotificationService = this.userNotificationService ?? new Mock<IUserNotificationService>();
        var applicationLifetime = this.applicationLifetime ?? this.CreateDefaultApplicationLifetime();
        var operationLifetime = this.operationLifetime ?? new Mock<IApplicationOperationLifetime>();
        var settingsLauncher = this.settingsLauncher ?? new Mock<ISettingsLauncher>();
        var externalLauncher = this.externalLauncher ?? new Mock<IExternalLauncher>();
        var processInspector = this.processInspector ?? this.CreateDefaultProcessInspector();

        var projectPane = new ProjectPaneController(
            projectService.Object,
            this.CreateProjectEnricher(logoCacheService),
            new ProjectModelCatalog(),
            projectDialogService.Object,
            new PromptState());

        var agentPane = new AgentPaneController(
            pluginCatalog.Object,
            agentDetectionService.Object,
            agentVersionService.Object,
            logoCacheService.Object,
            operationLifetime.Object,
            NullLogger<AgentPaneController>.Instance);

        var updateControl = new UpdateControlViewModel(
            updateWorkflow.Object,
            NullLogger<UpdateControlViewModel>.Instance,
            operationLifetime.Object);

        var statusCoordinator = new StatusMessageCoordinator(
            projectPane,
            agentPane,
            updateControl);

        var launchCoordinator = new LaunchCommandCoordinator(
            agentCommandWorkflow.Object,
            operationLifetime.Object,
            userNotificationService.Object,
            NullLogger<LaunchCommandCoordinator>.Instance);

        var processMonitor = new AgentProcessMonitor(
            new ProcessOrchestrator(
                Mock.Of<IRuntimeResolver>(),
                Mock.Of<ICommandLineBuilder>(),
                Mock.Of<IInteractiveProcessRunner>(),
                Mock.Of<IProcessOutputRunner>(),
                new AgentProcessFinder(processInspector.Object, new AgentProcessMatcher(), pluginCatalog.Object),
                NullLogger<ProcessOrchestrator>.Instance),
            pluginCatalog.Object,
            NullLogger<AgentProcessMonitor>.Instance);

        return new LaunchWindowViewModel(
            projectPane,
            agentPane,
            launchCoordinator,
            preferencesStore.Object,
            new LaunchWindowActionBuilder(),
            updateControl,
            settingsLauncher.Object,
            userNotificationService.Object,
            applicationLifetime.Object,
            externalLauncher.Object,
            statusCoordinator,
            processMonitor);
    }

    /// <summary>
    ///   Builds the ViewModel and returns it along with all mock services for verification.
    /// </summary>
    public (LaunchWindowViewModel ViewModel, TestMocks Mocks) BuildWithMocks()
    {
        var projectService = this.projectService ?? this.CreateDefaultProjectService();
        var pluginCatalog = this.pluginCatalog ?? this.CreateDefaultPluginCatalog();
        var agentDetectionService = this.agentDetectionService ?? new Mock<IAgentDetectionService>();
        var agentVersionService = this.agentVersionService ?? new Mock<IAgentVersionService>();
        var logoCacheService = this.logoCacheService ?? new Mock<ILogoCacheService>();
        var preferencesStore = this.preferencesStore ?? this.CreateDefaultPreferencesStore();
        var projectDialogService = this.projectDialogService ?? new Mock<IProjectDialogService>();
        var agentCommandWorkflow = this.agentCommandWorkflow ?? new Mock<IAgentCommandWorkflow>();
        var updateWorkflow = this.updateWorkflow ?? this.CreateDefaultUpdateWorkflow();
        var userNotificationService = this.userNotificationService ?? new Mock<IUserNotificationService>();
        var applicationLifetime = this.applicationLifetime ?? this.CreateDefaultApplicationLifetime();
        var operationLifetime = this.operationLifetime ?? new Mock<IApplicationOperationLifetime>();
        var settingsLauncher = this.settingsLauncher ?? new Mock<ISettingsLauncher>();
        var externalLauncher = this.externalLauncher ?? new Mock<IExternalLauncher>();
        var processInspector = this.processInspector ?? this.CreateDefaultProcessInspector();

        var projectPane = new ProjectPaneController(
            projectService.Object,
            this.CreateProjectEnricher(logoCacheService),
            new ProjectModelCatalog(),
            projectDialogService.Object,
            new PromptState());

        var agentPane = new AgentPaneController(
            pluginCatalog.Object,
            agentDetectionService.Object,
            agentVersionService.Object,
            logoCacheService.Object,
            operationLifetime.Object,
            NullLogger<AgentPaneController>.Instance);

        var updateControl = new UpdateControlViewModel(
            updateWorkflow.Object,
            NullLogger<UpdateControlViewModel>.Instance,
            operationLifetime.Object);

        var statusCoordinator = new StatusMessageCoordinator(
            projectPane,
            agentPane,
            updateControl);

        var launchCoordinator = new LaunchCommandCoordinator(
            agentCommandWorkflow.Object,
            operationLifetime.Object,
            userNotificationService.Object,
            NullLogger<LaunchCommandCoordinator>.Instance);

        var processMonitor = new AgentProcessMonitor(
            new ProcessOrchestrator(
                Mock.Of<IRuntimeResolver>(),
                Mock.Of<ICommandLineBuilder>(),
                Mock.Of<IInteractiveProcessRunner>(),
                Mock.Of<IProcessOutputRunner>(),
                new AgentProcessFinder(processInspector.Object, new AgentProcessMatcher(), pluginCatalog.Object),
                NullLogger<ProcessOrchestrator>.Instance),
            pluginCatalog.Object,
            NullLogger<AgentProcessMonitor>.Instance);

        var viewModel = new LaunchWindowViewModel(
            projectPane,
            agentPane,
            launchCoordinator,
            preferencesStore.Object,
            new LaunchWindowActionBuilder(),
            updateControl,
            settingsLauncher.Object,
            userNotificationService.Object,
            applicationLifetime.Object,
            externalLauncher.Object,
            statusCoordinator,
            processMonitor);

        var mocks = new TestMocks
        {
            ProjectService = projectService,
            PluginCatalog = pluginCatalog,
            AgentDetectionService = agentDetectionService,
            AgentVersionService = agentVersionService,
            LogoCacheService = logoCacheService,
            PreferencesStore = preferencesStore,
            ProjectDialogService = projectDialogService,
            AgentCommandWorkflow = agentCommandWorkflow,
            UpdateWorkflow = updateWorkflow,
            UserNotificationService = userNotificationService,
            ApplicationLifetime = applicationLifetime,
            OperationLifetime = operationLifetime,
            SettingsLauncher = settingsLauncher,
            ExternalLauncher = externalLauncher,
            ProcessInspector = processInspector
        };

        return (viewModel, mocks);
    }

    private Mock<IProjectService> CreateDefaultProjectService()
    {
        var mock = new Mock<IProjectService>();
        mock.Setup(x => x.GetAllProjects()).Returns(this.projects ?? Array.Empty<Project>());
        mock.Setup(x => x.GetCurrentProject()).Returns(this.currentProject);
        return mock;
    }

    /// <summary>
    ///   Creates the real Core enricher over the logo cache mock: cache misses resolve to no
    ///   logo (the options carry no default path), so project rows get null logo paths unless
    ///   a test configures the cache.
    /// </summary>
    private ProjectEnricher CreateProjectEnricher(Mock<ILogoCacheService> logoCacheService) =>
            new(new ProjectLogoResolver(logoCacheService.Object, new LogoOptions(null)));

    private Mock<IPluginCatalog> CreateDefaultPluginCatalog()
    {
        var mock = new Mock<IPluginCatalog>();
        mock.Setup(x => x.GetAllPlugins()).Returns(this.plugins ?? Array.Empty<Plugin>());
        return mock;
    }

    private Mock<IPreferencesStore> CreateDefaultPreferencesStore()
    {
        var mock = new Mock<IPreferencesStore>();
        mock.Setup(x => x.Load()).Returns(this.preferences ?? new AppPreferences());
        return mock;
    }

    private Mock<IUpdateWorkflow> CreateDefaultUpdateWorkflow()
    {
        var mock = new Mock<IUpdateWorkflow>();
        mock.Setup(x => x.GetCurrentVersion()).Returns(this.currentVersion ?? "1.0.0");
        return mock;
    }

    private Mock<IApplicationLifetime> CreateDefaultApplicationLifetime()
    {
        var mock = new Mock<IApplicationLifetime>();
        mock.Setup(x => x.Shutdown());
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
    public class TestMocks
    {
        public required Mock<IProjectService> ProjectService { get; init; }
        public required Mock<IPluginCatalog> PluginCatalog { get; init; }
        public required Mock<IAgentDetectionService> AgentDetectionService { get; init; }
        public required Mock<IAgentVersionService> AgentVersionService { get; init; }
        public required Mock<ILogoCacheService> LogoCacheService { get; init; }
        public required Mock<IPreferencesStore> PreferencesStore { get; init; }
        public required Mock<IProjectDialogService> ProjectDialogService { get; init; }
        public required Mock<IAgentCommandWorkflow> AgentCommandWorkflow { get; init; }
        public required Mock<IUpdateWorkflow> UpdateWorkflow { get; init; }
        public required Mock<IUserNotificationService> UserNotificationService { get; init; }
        public required Mock<IApplicationLifetime> ApplicationLifetime { get; init; }
        public required Mock<IApplicationOperationLifetime> OperationLifetime { get; init; }
        public required Mock<ISettingsLauncher> SettingsLauncher { get; init; }
        public required Mock<IExternalLauncher> ExternalLauncher { get; init; }
        public required Mock<IAgentProcessInspector> ProcessInspector { get; init; }
    }
}
