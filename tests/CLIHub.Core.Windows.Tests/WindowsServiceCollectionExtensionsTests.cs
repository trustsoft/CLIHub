namespace CLIHub.Core.Windows.Tests;

using Microsoft.Extensions.DependencyInjection;

using CLIHub.Core.Agents;
using CLIHub.Core;
using CLIHub.Core.Infrastructure.Processes;

public class WindowsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWindowsInfrastructure_CombinedWithCoreServices_ResolvesFullGraph()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices();
        services.AddWindowsInfrastructure();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IInteractiveProcessRunner>());
        Assert.NotNull(provider.GetRequiredService<IProcessOutputRunner>());
        Assert.NotNull(provider.GetRequiredService<IRuntimeExecutableLocator>());
        Assert.NotNull(provider.GetRequiredService<IAgentProcessInspector>());
        Assert.NotNull(provider.GetRequiredService<IAgentCommandService>());
        Assert.NotNull(provider.GetRequiredService<IAgentVersionService>());
        Assert.NotNull(provider.GetRequiredService<IStartupService>());
    }

    [Fact]
    public void AddWindowsInfrastructure_RegistersSingleInstanceGuardAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddClIHubCoreServices();
        services.AddWindowsInfrastructure();

        var guardDescriptor = services.Single(d => d.ServiceType == typeof(SingleInstanceGuard));
        var portDescriptor = services.Single(d => d.ServiceType == typeof(ISingleInstanceGuard));

        Assert.Equal(ServiceLifetime.Singleton, guardDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, portDescriptor.Lifetime);
        Assert.Equal(typeof(SingleInstanceGuard), guardDescriptor.ImplementationType);
    }

    [Fact]
    public void AddWindowsInfrastructure_PreAcquiredGuard_RegistersInstanceAndResolvesSameGuard()
    {
        var guard = new SingleInstanceGuard(
            $@"Local\CLIHub.Tests.{Guid.NewGuid():N}",
            $"CLIHub.Tests.{Guid.NewGuid():N}");

        var services = new ServiceCollection();
        services.AddClIHubCoreServices();
        services.AddWindowsInfrastructure(guard);

        var guardDescriptor = services.Single(d => d.ServiceType == typeof(SingleInstanceGuard));
        var portDescriptor = services.Single(d => d.ServiceType == typeof(ISingleInstanceGuard));

        Assert.Equal(ServiceLifetime.Singleton, guardDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, portDescriptor.Lifetime);

        using var provider = services.BuildServiceProvider();

        Assert.Same(guard, provider.GetRequiredService<SingleInstanceGuard>());
        Assert.Same(guard, provider.GetRequiredService<ISingleInstanceGuard>());
    }
}
