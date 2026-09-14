using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class UpdateServiceTests
{
    private readonly FakeUpdateClient _client = new();

    private UpdateService CreateService(string configDirectory, bool? checkOnStartup = null)
    {
        var store = new ConfigStore(
            new PhysicalFileSystem(),
            new StubPathProvider(configDirectory, configDirectory));

        if (checkOnStartup is not null)
        {
            var config = store.Load();
            config.Update.CheckOnStartup = checkOnStartup;
            store.Save(config);
        }

        return new UpdateService(_client, store);
    }

    [Fact]
    public async Task Check_NotInstalled_SkipsSilently()
    {
        using var temp = new TempDirectory();
        _client.IsInstalled = false;
        var service = CreateService(temp.Path);

        var result = await service.RunStartupCheckAsync();

        Assert.False(result);
        Assert.Equal(0, _client.CheckCalls);
    }

    [Fact]
    public async Task Check_DisabledInConfig_Skips()
    {
        using var temp = new TempDirectory();
        var service = CreateService(temp.Path, checkOnStartup: false);

        var result = await service.RunStartupCheckAsync();

        Assert.False(result);
        Assert.Equal(0, _client.CheckCalls);
    }

    [Fact]
    public async Task Check_NoUpdate_DoesNotDownloadOrNotify()
    {
        using var temp = new TempDirectory();
        _client.CheckResult = UpdateCheckResult.None;
        var service = CreateService(temp.Path);
        var ready = false;
        service.UpdateReady += _ => ready = true;

        var result = await service.RunStartupCheckAsync();

        Assert.False(result);
        Assert.Equal(1, _client.CheckCalls);
        Assert.Equal(0, _client.DownloadCalls);
        Assert.False(ready);
    }

    [Fact]
    public async Task Check_UpdateAvailable_DownloadsAndRaisesReady()
    {
        using var temp = new TempDirectory();
        _client.CheckResult = UpdateCheckResult.Found("1.2.3");
        var service = CreateService(temp.Path);
        string? readyVersion = null;
        service.UpdateReady += version => readyVersion = version;

        var result = await service.RunStartupCheckAsync();

        Assert.True(result);
        Assert.Equal(1, _client.DownloadCalls);
        Assert.Equal("1.2.3", readyVersion);
    }

    [Fact]
    public async Task Check_DownloadFailure_RaisesNothing()
    {
        using var temp = new TempDirectory();
        _client.CheckResult = UpdateCheckResult.Found("1.2.3");
        _client.DownloadResult = false;
        var service = CreateService(temp.Path);
        var ready = false;
        service.UpdateReady += _ => ready = true;

        var result = await service.RunStartupCheckAsync();

        Assert.False(result);
        Assert.False(ready);
    }

    [Fact]
    public async Task Check_ClientThrows_SilentFalse()
    {
        using var temp = new TempDirectory();
        _client.CheckException = new InvalidOperationException("offline");
        var service = CreateService(temp.Path);

        var result = await service.RunStartupCheckAsync();

        Assert.False(result);
    }

    [Fact]
    public void Apply_DelegatesToClient()
    {
        using var temp = new TempDirectory();
        var service = CreateService(temp.Path);

        service.Apply();

        Assert.Equal(1, _client.ApplyCalls);
    }
}
