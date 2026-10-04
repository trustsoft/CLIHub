namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Services;

public class StartupServiceTests
{
    private const string ExePath = @"C:\Program Files\CLIHub\CLIHub.exe";

    private readonly FakeStartupRegistry _registry = new();

    private StartupService CreateService(Func<string?>? pathProvider = null) =>
        new(_registry, NullLogger<StartupService>.Instance, pathProvider ?? (() => ExePath));

    [Fact]
    public void IsEnabled_NoRegistration_ReturnsFalse()
    {
        Assert.False(CreateService().IsEnabled());
    }

    [Fact]
    public void SetEnabled_True_StoresQuotedExecutablePath()
    {
        Assert.True(CreateService().SetEnabled(true));

        Assert.True(_registry.Values.TryGetValue(StartupService.ValueName, out var value));
        Assert.Equal($"\"{ExePath}\"", value);
    }

    [Fact]
    public void IsEnabled_AfterEnable_ReturnsTrue()
    {
        var service = CreateService();
        service.SetEnabled(true);

        Assert.True(service.IsEnabled());
    }

    [Fact]
    public void SetEnabled_False_RemovesRegistration()
    {
        var service = CreateService();
        service.SetEnabled(true);

        Assert.True(service.SetEnabled(false));
        Assert.False(service.IsEnabled());
    }

    [Fact]
    public void SetEnabled_UnknownExecutablePath_ReturnsFalseAndDoesNotRegister()
    {
        var service = CreateService(() => null);

        Assert.False(service.SetEnabled(true));
        Assert.False(service.IsEnabled());
    }

    [Fact]
    public void SetEnabled_RegistryError_ReturnsFalseWithoutThrowing()
    {
        _registry.ThrowOnAccess = true;

        Assert.False(CreateService().SetEnabled(true));
        Assert.False(CreateService().IsEnabled());
    }
}
