namespace CLIHub.Core.Windows.Tests;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Windows;

public class StartupServiceTests
{
    private const string ExePath = @"C:\Program Files\CLIHub\CLIHub.exe";

    private readonly FakeStartupRegistry registry = new();

    private StartupService CreateService(Func<string?>? pathProvider = null) =>
        new(this.registry, NullLogger<StartupService>.Instance, pathProvider ?? (() => ExePath));

    [Fact]
    public void IsEnabled_NoRegistration_ReturnsFalse()
    {
        Assert.False(this.CreateService().IsEnabled());
    }

    [Fact]
    public void SetEnabled_True_StoresQuotedExecutablePath()
    {
        Assert.True(this.CreateService().SetEnabled(true));

        Assert.True(this.registry.Values.TryGetValue(StartupService.ValueName, out var value));
        Assert.Equal($"\"{ExePath}\"", value);
    }

    [Fact]
    public void IsEnabled_AfterEnable_ReturnsTrue()
    {
        var service = this.CreateService();
        service.SetEnabled(true);

        Assert.True(service.IsEnabled());
    }

    [Fact]
    public void SetEnabled_False_RemovesRegistration()
    {
        var service = this.CreateService();
        service.SetEnabled(true);

        Assert.True(service.SetEnabled(false));
        Assert.False(service.IsEnabled());
    }

    [Fact]
    public void SetEnabled_UnknownExecutablePath_ReturnsFalseAndDoesNotRegister()
    {
        var service = this.CreateService(() => null);

        Assert.False(service.SetEnabled(true));
        Assert.False(service.IsEnabled());
    }

    [Fact]
    public void SetEnabled_RegistryError_ReturnsFalseWithoutThrowing()
    {
        this.registry.ThrowOnAccess = true;

        Assert.False(this.CreateService().SetEnabled(true));
        Assert.False(this.CreateService().IsEnabled());
    }
}
