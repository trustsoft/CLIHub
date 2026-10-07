namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

using CLIHub;
using CLIHub.Core;
using CLIHub.Core.Infrastructure.Windows;

public class ApplicationLifetimeRegistrationTests
{
    [Fact]
    public void AddClIHubServices_RegistersWpfApplicationLifetime()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<WpfApplicationLifetime>(provider.GetRequiredService<IApplicationLifetime>());
    }

    [Fact]
    public void AddClIHubServices_RegistersSingleInstanceGuardAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        var registration = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(SingleInstanceGuard));

        Assert.Equal(ServiceLifetime.Singleton, registration.Lifetime);
        Assert.Equal(typeof(SingleInstanceGuard), registration.ImplementationType);

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<SingleInstanceGuard>();
        var second = provider.GetRequiredService<SingleInstanceGuard>();

        Assert.Same(first, second);
    }
}
