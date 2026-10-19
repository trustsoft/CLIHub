namespace CLIHub.Tests.Composition;

using Microsoft.Extensions.DependencyInjection;

using CLIHub.Composition;
using CLIHub.Converters;
using CLIHub.Core.Updates;
using CLIHub.Hotkeys;
using CLIHub.Imaging;
using CLIHub.Launching;
using CLIHub.Lifetime;
using CLIHub.Settings;
using CLIHub.Startup;
using CLIHub.Tests.Services;
using CLIHub.Tray;
using CLIHub.Updates;
using CLIHub.ViewModels;
using CLIHub.Views;

public class ServiceRegistrationTests
{
    [Fact]
    public void AddClIHubServices_ResolvesWpfSafeServiceGraph()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();
        services.ReplaceGuardWithUniqueNames();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IApplicationLifetime>());
        Assert.NotNull(provider.GetRequiredService<IApplicationOperationLifetime>());
        Assert.NotNull(provider.GetRequiredService<IInstanceCoordinator>());
        Assert.NotNull(provider.GetRequiredService<IStartupStateLoader>());
        Assert.NotNull(provider.GetRequiredService<IOptionalStartupCoordinator>());
        Assert.NotNull(provider.GetRequiredService<IApplicationSession>());
        Assert.NotNull(provider.GetRequiredService<IApplicationBootstrapper>());
        Assert.NotNull(provider.GetRequiredService<IUpdateStartupCoordinator>());
        Assert.NotNull(provider.GetRequiredService<IReleaseNotesStartupCoordinator>());
        Assert.NotNull(provider.GetRequiredService<IAgentCommandWorkflow>());
        Assert.NotNull(provider.GetRequiredService<PromptState>());
        Assert.NotNull(provider.GetRequiredService<ProjectModelCatalog>());
        Assert.NotNull(provider.GetRequiredService<LaunchCommandCoordinator>());
        Assert.NotNull(provider.GetRequiredService<LaunchWindowActionBuilder>());
        Assert.NotNull(provider.GetRequiredService<ProjectPaneController>());
        Assert.NotNull(provider.GetRequiredService<AgentPaneController>());
        Assert.NotNull(provider.GetRequiredService<UpdateControlViewModel>());
        Assert.NotNull(provider.GetRequiredService<StatusMessageCoordinator>());
        Assert.NotNull(provider.GetRequiredService<LaunchWindowViewModel>());
        Assert.NotNull(provider.GetRequiredService<ITrayActions>());
        Assert.NotNull(provider.GetRequiredService<IProjectDialogService>());
        Assert.NotNull(provider.GetRequiredService<IUserNotificationService>());
        Assert.NotNull(provider.GetRequiredService<IExternalLauncher>());
        Assert.NotNull(provider.GetRequiredService<LogoImageSourceCache>());
        Assert.NotNull(provider.GetRequiredService<PathToImageConverter>());
        Assert.NotNull(provider.GetRequiredService<IRuntimePreferenceTarget>());
        Assert.NotNull(provider.GetRequiredService<IAccentPreferenceTarget>());
        Assert.NotNull(provider.GetRequiredService<IAgentManagementSection>());
        Assert.NotNull(provider.GetRequiredService<IProjectRegistrySection>());
        Assert.NotNull(provider.GetRequiredService<AccentSelectionMemory>());
        Assert.NotNull(provider.GetRequiredService<ISettingsLauncher>());
        Assert.NotNull(provider.GetRequiredService<IUpdateWorkflow>());
        Assert.NotNull(provider.GetRequiredService<WhatsNewViewModel>());
        Assert.NotNull(provider.GetRequiredService<IReleaseNotesLauncher>());
        Assert.NotNull(provider.GetRequiredService<IUpdateVersionProvider>());
    }

    [Fact]
    public void AddClIHubServices_MapsPathDisplayStyleTargetToLaunchWindowViewModel()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        using var provider = services.BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<LaunchWindowViewModel>(),
            provider.GetRequiredService<IPathDisplayStyleTarget>());
    }

    [Fact]
    public void AddClIHubServices_SingletonPorts_ResolveToTheSameInstance()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();
        services.ReplaceGuardWithUniqueNames();

        using var provider = services.BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<IUpdateWorkflow>(),
            provider.GetRequiredService<IUpdateWorkflow>());
        Assert.Same(
            provider.GetRequiredService<IInstanceCoordinator>(),
            provider.GetRequiredService<IInstanceCoordinator>());
        Assert.Same(
            provider.GetRequiredService<IApplicationSession>(),
            provider.GetRequiredService<IApplicationSession>());
        Assert.Same(
            provider.GetRequiredService<ITrayActions>(),
            provider.GetRequiredService<ITrayActions>());
        Assert.Same(
            provider.GetRequiredService<ISettingsLauncher>(),
            provider.GetRequiredService<ISettingsLauncher>());
        Assert.Same(
            provider.GetRequiredService<IReleaseNotesLauncher>(),
            provider.GetRequiredService<IReleaseNotesLauncher>());
        Assert.Same(
            provider.GetRequiredService<IUpdateVersionProvider>(),
            provider.GetRequiredService<IUpdateVersionProvider>());
    }

    [Fact]
    public void AddClIHubServices_TransientRegistrations_ResolveNewInstances()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        var agentManagementSection = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(IAgentManagementSection));
        var projectRegistrySection = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(IProjectRegistrySection));
        var settingsViewModel = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(SettingsViewModel));

        Assert.Equal(ServiceLifetime.Transient, agentManagementSection.Lifetime);
        Assert.Equal(ServiceLifetime.Transient, projectRegistrySection.Lifetime);
        Assert.Equal(ServiceLifetime.Transient, settingsViewModel.Lifetime);

        using var provider = services.BuildServiceProvider();

        Assert.NotSame(
            provider.GetRequiredService<IAgentManagementSection>(),
            provider.GetRequiredService<IAgentManagementSection>());
        Assert.NotSame(
            provider.GetRequiredService<IProjectRegistrySection>(),
            provider.GetRequiredService<IProjectRegistrySection>());
    }

    [Fact]
    public void AddClIHubServices_WindowAndHotkeyBoundServices_AreRegisteredWithoutConstruction()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        var launchWindow = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(LaunchWindow));
        var settingsWindow = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(SettingsWindow));
        var whatsNewWindow = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(WhatsNewWindow));
        var hotkeyService = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(GlobalHotkeyService));
        var hotkeyPort = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(IGlobalHotkeyService));
        var startupUi = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(IApplicationStartupUi));
        var trayIconController = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(TrayIconController));
        var trayHost = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ITrayHost));
        var preferenceApplier = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(IPreferenceApplier));
        var settingsApplication = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(SettingsApplicationService));
        var settingsViewModel = Assert.Single(
            services, descriptor => descriptor.ServiceType == typeof(SettingsViewModel));

        Assert.Equal(ServiceLifetime.Singleton, launchWindow.Lifetime);
        Assert.Equal(typeof(LaunchWindow), launchWindow.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, settingsWindow.Lifetime);
        Assert.Equal(typeof(SettingsWindow), settingsWindow.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, whatsNewWindow.Lifetime);
        Assert.Equal(typeof(WhatsNewWindow), whatsNewWindow.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, hotkeyService.Lifetime);
        Assert.NotNull(hotkeyService.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Singleton, hotkeyPort.Lifetime);
        Assert.NotNull(hotkeyPort.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Singleton, startupUi.Lifetime);
        Assert.Equal(typeof(WpfApplicationStartupUi), startupUi.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, trayIconController.Lifetime);
        Assert.Equal(typeof(TrayIconController), trayIconController.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, trayHost.Lifetime);
        Assert.NotNull(trayHost.ImplementationFactory);
        Assert.Equal(ServiceLifetime.Singleton, preferenceApplier.Lifetime);
        Assert.Equal(typeof(PreferenceApplier), preferenceApplier.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, settingsApplication.Lifetime);
        Assert.Equal(typeof(SettingsApplicationService), settingsApplication.ImplementationType);
        Assert.Equal(ServiceLifetime.Transient, settingsViewModel.Lifetime);
        Assert.NotNull(settingsViewModel.ImplementationFactory);
    }

    [Fact]
    public void AddClIHubServices_FuncAccessors_AreRegisteredAsSingletonFactories()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        foreach (var serviceType in new[]
                 {
                     typeof(Func<IApplicationStartupUi>),
                     typeof(Func<IHotkeyStartupRegistrar>),
                     typeof(Func<SettingsWindow>),
                     typeof(Func<IUpdateRequestSource>),
                     typeof(Func<WhatsNewWindow>)
                 })
        {
            var registration = Assert.Single(services, descriptor => descriptor.ServiceType == serviceType);

            Assert.Equal(ServiceLifetime.Singleton, registration.Lifetime);
            Assert.NotNull(registration.ImplementationFactory);
        }
    }
}
