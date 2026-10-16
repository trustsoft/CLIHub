namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

using CLIHub.Core.Windows;
using CLIHub.Lifetime;
using CLIHub.Composition;

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

        services.ReplaceGuardWithUniqueNames();

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<SingleInstanceGuard>();
        var second = provider.GetRequiredService<SingleInstanceGuard>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddClIHubServices_PreAcquiredGuard_DisposedByProvider()
    {
        var guard = new SingleInstanceGuard(
            $@"Local\CLIHub.Tests.{Guid.NewGuid():N}",
            $"CLIHub.Tests.{Guid.NewGuid():N}");
        var services = new ServiceCollection();
        services.AddClIHubServices(guard);

        using (var provider = services.BuildServiceProvider())
        {
            Assert.Same(guard, provider.GetRequiredService<ISingleInstanceGuard>());
            Assert.False(guard.IsDisposed);
        }

        Assert.True(guard.IsDisposed);
    }

    [Fact]
    public void AddClIHubServices_PreAcquiredGuard_SameInstanceAcrossResolutions()
    {
        var guard = new SingleInstanceGuard(
            $@"Local\CLIHub.Tests.{Guid.NewGuid():N}",
            $"CLIHub.Tests.{Guid.NewGuid():N}");

        var services = new ServiceCollection();
        services.AddClIHubServices(guard);

        using var provider = services.BuildServiceProvider();

        Assert.Same(guard, provider.GetRequiredService<SingleInstanceGuard>());
        Assert.Same(
            provider.GetRequiredService<ISingleInstanceGuard>(),
            provider.GetRequiredService<ISingleInstanceGuard>());
    }
}
