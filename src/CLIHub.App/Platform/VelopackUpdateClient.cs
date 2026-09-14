using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;
using Velopack;
using Velopack.Sources;

namespace CLIHub.App.Platform;

public sealed class VelopackUpdateClient : IUpdateClient
{
    public const string FeedUrl = "https://github.com/trustsoft/CLIHub";
    public const string FeedOverrideVariable = "CLIHUB_UPDATE_FEED";

    private readonly UpdateManager _manager;
    private UpdateInfo? _pending;

    public VelopackUpdateClient()
        : this(CreateManager())
    {
    }

    public VelopackUpdateClient(UpdateManager manager)
    {
        _manager = manager;
    }

    public bool IsInstalled => _manager.IsInstalled;

    public async Task<UpdateCheckResult> CheckAsync()
    {
        var update = await _manager.CheckForUpdatesAsync();
        _pending = update;

        return update is null
            ? UpdateCheckResult.None
            : UpdateCheckResult.Found(update.TargetFullRelease.Version.ToString());
    }

    public async Task<bool> DownloadAsync()
    {
        if (_pending is null)
        {
            return false;
        }

        await _manager.DownloadUpdatesAsync(_pending);
        return true;
    }

    public void ApplyAndRestart()
    {
        if (_pending is not null)
        {
            _manager.ApplyUpdatesAndRestart(_pending.TargetFullRelease);
        }
    }

    private static UpdateManager CreateManager()
    {
        var feed = Environment.GetEnvironmentVariable(FeedOverrideVariable);
        if (!string.IsNullOrWhiteSpace(feed))
        {
            return new UpdateManager(CreateOverrideSource(feed));
        }

        return new UpdateManager(new GithubSource(FeedUrl, null, false));
    }

    private static IUpdateSource CreateOverrideSource(string feed)
    {
        if (Uri.TryCreate(feed, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return new SimpleWebSource(feed);
        }

        return new SimpleFileSource(new DirectoryInfo(feed));
    }
}
