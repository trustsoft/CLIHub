namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

using CLIHub.Tray;
using CLIHub.Composition;

public class TrayMenuBuilderRegistrationTests
{
    [Fact]
    public void AddClIHubServices_RegistersTrayMenuBuilderAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddClIHubServices();

        var descriptor = services.Single(service => service.ServiceType == typeof(TrayMenuBuilder));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(TrayMenuBuilder), descriptor.ImplementationType);
    }

    [Fact]
    public void AddClIHubServices_RegistersTrayActions()
    {
        var services = new ServiceCollection();

        services.AddClIHubServices();

        var actions = services.Single(service => service.ServiceType == typeof(ITrayActions));

        Assert.Equal(ServiceLifetime.Singleton, actions.Lifetime);
        Assert.Equal(typeof(TrayActions), actions.ImplementationType);
    }
}
