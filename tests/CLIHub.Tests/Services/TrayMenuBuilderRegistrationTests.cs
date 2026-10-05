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
}
