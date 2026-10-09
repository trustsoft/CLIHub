namespace CLIHub.Tests.Builders;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Services;
using CLIHub.Core.Updates;
using CLIHub.ViewModels;

/// <summary>
///   Fluent builder for LaunchWindowViewModel test instances with sensible defaults.
/// </summary>
public class LaunchWindowViewModelBuilder
{
    private Mock<IProjectService>? _projectService;
    private Mock<IPluginCatalog>? _pluginCatalog;
    private Mock<IAgentDetectionService>? _agentDetectionService;
    private Mock<IAgentVersionService>? _agentVersionService;
    private Mock<ILogoCacheService>? _logoCacheService;
    private Mock<IPreferencesStore>? _preferencesStore;
    private Mock<IProjectDialogService>? _projectDialogService;
    private Mock<IAgentCommandWorkflow>? _agentCommandWorkflow;
    private Mock<IUpdateWorkflow>? _updateWorkflow;
    private Mock<IUserNotificationService>? _userNotificationService;
    private Mock<IApplicationLifetime>? _applicationLifetime;
    private Mock<IApplicationOperationLifetime>? _operationLifetime;
    private Mock<ISettingsLauncher>? _settingsLauncher;
    private Mock<IExternalLauncher>? _externalLauncher;
    private Mock<IAgentProcessInspector>? _processInspector;

    private Project[]? _projects;
    private Project? _currentProject;
    private Plugin[]? _plugins;
    private AppPreferences? _preferences;
    private string? _currentVersion;

    /// <summary>
    ///   Configures the project service with specific projects.
    /// </summary>
    public LaunchWindowViewModelBuilder WithProjects(params Project[] projects)
    {
        _projects = projects;
        return this;
    }

    /// <summary>
    ///   Configures the current project selection.
    /// </summary>
    public LaunchWindowViewModelBuilder WithCurrentProject(Project? project)
    {
        _currentProject = project;
        return this;
    }

    /// <summary>
    ///   Configures the plugin catalog with specific plugins.
    /// </summary>
    public LaunchWindowViewModelBuilder WithPlugins(params Plugin[] plugins)
    {
        _plugins = plugins;
        return this;
    }

    /// <summary>
    ///   Configures application preferences.
    /// </summary>
    public LaunchWindowViewModelBuilder WithPreferences(AppPreferences preferences)
    {
        _preferences = preferences;
        return this;
    }

    /// <summary>
    ///   Provides a custom project service mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithProjectService(Mock<IProjectService> projectService)
    {
        _projectService = projectService;
        return this;
    }

    /// <summary>
    ///   Provides a custom plugin catalog mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithPluginCatalog(Mock<IPluginCatalog> pluginCatalog)
    {
        _pluginCatalog = pluginCatalog;
        return this;
    }

    /// <summary>
    ///   Provides a custom project dialog service mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithProjectDialogService(Mock<IProjectDialogService> dialogService)
    {
        _projectDialogService = dialogService;
        return this;
    }

    /// <summary>
    ///   Provides a custom user notification service mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithUserNotificationService(Mock<IUserNotificationService> notificationService)
    {
        _userNotificationService = notificationService;
        return this;
    }

    /// <summary>
    ///   Provides a custom application lifetime mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithApplicationLifetime(Mock<IApplicationLifetime> lifetime)
    {
        _applicationLifetime = lifetime;
        return this;
    }

    /// <summary>
    ///   Provides a custom update workflow mock.
    /// </summary>
    public LaunchWindowViewModelBuilder WithUpdateWorkflow(Mock<IUpdateWorkflow> updateWorkflow)
    {
        _updateWorkflow = updateWorkflow;
        return this;
    }

    /// <summary>
    ///   Builds a LaunchWindowViewModel instance with all dependencies configured.
    /// </summary>
    public LaunchWindowViewModel Build()
    {
        var projectService = _projectService ?? CreateDefaultProjectService();
        var pluginCatalog = _pluginCatalog ?? CreateDefaultPluginCatalog();
        var agentDetectionService = _agentDetectionService ?? new Mock<IAgentDetectionService>();
        var agentVersionService = _agentVersionService ?? new Mock<IAgentVersionService>();
        var logoCacheService = _logoCacheService ?? new Mock<ILogoCacheService>();
        var preferencesStore = _preferencesStore ?? CreateDefaultPreferencesStore();
        var projectDialogService = _projectDialogService ?? new Mock<IProjectDialogService>();
        var agentCommandWorkflow = _agentCommandWorkflow ?? new Mock<IAgentCommandWorkflow>();
        var updateWorkflow = _updateWorkflow ?? CreateDefaultUpdateWorkflow();
        var userNotificationService = _userNotificationService ?? new Mock<IUserNotificationService>();
        var applicationLifetime = _applicationLifetime ?? CreateDefaultApplicationLifetime();
        var operationLifetime = _operationLifetime ?? new Mock<IApplicationOperationLifetime>();
        var settingsLauncher = _settingsLauncher ?? new Mock<ISettingsLauncher>();
        var externalLauncher = _externalLauncher ?? new Mock<IExternalLauncher>();
        var processInspector = _processInspector ?? CreateDefaultProcessInspector();

        var projectPane = new ProjectPaneController(
            projectService.Object,
            projectDialogService.Object,
            new PromptState());

        var agentPane = new AgentPaneController(
            pluginCatalog.Object,
            agentDetectionService.Object,
            agentVersionService.Object,
            logoCacheService.Object,
            operationLifetime.Object);

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

        var matcher = new AgentProcessMatcher(NullLogger<AgentProcessMatcher>.Instance);
        var processMonitor = new AgentProcessMonitor(
            processInspector.Object,
            matcher,
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
        var projectService = _projectService ?? CreateDefaultProjectService();
        var pluginCatalog = _pluginCatalog ?? CreateDefaultPluginCatalog();
        var agentDetectionService = _agentDetectionService ?? new Mock<IAgentDetectionService>();
        var agentVersionService = _agentVersionService ?? new Mock<IAgentVersionService>();
        var logoCacheService = _logoCacheService ?? new Mock<ILogoCacheService>();
        var preferencesStore = _preferencesStore ?? CreateDefaultPreferencesStore();
        var projectDialogService = _projectDialogService ?? new Mock<IProjectDialogService>();
        var agentCommandWorkflow = _agentCommandWorkflow ?? new Mock<IAgentCommandWorkflow>();
        var updateWorkflow = _updateWorkflow ?? CreateDefaultUpdateWorkflow();
        var userNotificationService = _userNotificationService ?? new Mock<IUserNotificationService>();
        var applicationLifetime = _applicationLifetime ?? CreateDefaultApplicationLifetime();
        var operationLifetime = _operationLifetime ?? new Mock<IApplicationOperationLifetime>();
        var settingsLauncher = _settingsLauncher ?? new Mock<ISettingsLauncher>();
        var externalLauncher = _externalLauncher ?? new Mock<IExternalLauncher>();
        var processInspector = _processInspector ?? CreateDefaultProcessInspector();

        var projectPane = new ProjectPaneController(
            projectService.Object,
            projectDialogService.Object,
            new PromptState());

        var agentPane = new AgentPaneController(
            pluginCatalog.Object,
            agentDetectionService.Object,
            agentVersionService.Object,
            logoCacheService.Object,
            operationLifetime.Object);

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

        var matcher = new AgentProcessMatcher(NullLogger<AgentProcessMatcher>.Instance);
        var processMonitor = new AgentProcessMonitor(
            processInspector.Object,
            matcher,
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
        mock.Setup(x => x.GetAllProjects()).Returns(_projects ?? Array.Empty<Project>());
        mock.Setup(x => x.GetCurrentProject()).Returns(_currentProject);
        return mock;
    }

    private Mock<IPluginCatalog> CreateDefaultPluginCatalog()
    {
        var mock = new Mock<IPluginCatalog>();
        mock.Setup(x => x.GetAllPlugins()).Returns(_plugins ?? Array.Empty<Plugin>());
        return mock;
    }

    private Mock<IPreferencesStore> CreateDefaultPreferencesStore()
    {
        var mock = new Mock<IPreferencesStore>();
        mock.Setup(x => x.Load()).Returns(_preferences ?? new AppPreferences());
        return mock;
    }

    private Mock<IUpdateWorkflow> CreateDefaultUpdateWorkflow()
    {
        var mock = new Mock<IUpdateWorkflow>();
        mock.Setup(x => x.GetCurrentVersion()).Returns(_currentVersion ?? "1.0.0");
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
        mock.Setup(x => x.GetRunningProcesses()).Returns([]);
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
