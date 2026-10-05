namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

using CLIHub;
using CLIHub.Core;

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
}
