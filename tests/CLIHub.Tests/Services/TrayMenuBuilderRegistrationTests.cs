namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

using CLIHub;

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
    public void AddClIHubServices_RegistersTrayActionsAndHostPorts()
    {
        var services = new ServiceCollection();

        services.AddClIHubServices();

        var actions = services.Single(service => service.ServiceType == typeof(ITrayActions));
        var host = services.Single(service => service.ServiceType == typeof(ITrayHost));

        Assert.Equal(ServiceLifetime.Singleton, actions.Lifetime);
        Assert.Equal(typeof(TrayActions), actions.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, host.Lifetime);
        Assert.NotNull(host.ImplementationFactory);
    }
}
