namespace CLIHub.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using CLIHub;
using CLIHub.Core;
using CLIHub.Core.Plugins;

public class PluginInitializationServiceTests
{
    [Fact]
    public void Initialize_SeedsBeforeLoadingPlugins()
    {
        var sequence = new MockSequence();
        var seeder = new Mock<IPluginSeeder>(MockBehavior.Strict);
        var manager = new Mock<IPluginManager>(MockBehavior.Strict);

        seeder.InSequence(sequence).Setup(x => x.SeedIfEmpty()).Returns(6);
        manager.InSequence(sequence).Setup(x => x.LoadPlugins());

        var service = new PluginInitializationService(seeder.Object, manager.Object);

        service.Initialize();

        seeder.Verify(x => x.SeedIfEmpty(), Times.Once);
        manager.Verify(x => x.LoadPlugins(), Times.Once);
    }

    [Fact]
    public void AddClIHubServices_RegistersPluginInitializationService()
    {
        var services = new ServiceCollection();
        services.AddClIHubServices();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<PluginInitializationService>(provider.GetRequiredService<IPluginInitializationService>());
    }
}
