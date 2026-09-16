using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed class UpdateService
{
    private readonly IUpdateClient _client;
    private readonly SettingsStore _settings;
    private readonly object _gate = new();
    private Task<UpdateCheckOutcome>? _inFlight;

    public UpdateService(IUpdateClient client, SettingsStore settings)
    {
        _client = client;
        _settings = settings;
    }

    public event Action<string>? UpdateReady;

    public async Task<bool> RunStartupCheckAsync()
    {
        try
        {
            if (!_client.IsInstalled || !_settings.Update.EffectiveCheckOnStartup)
            {
                return false;
            }

            return await RunSharedCheckAsync() == UpdateCheckOutcome.Ready;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public Task<UpdateCheckOutcome> CheckNowAsync()
    {
        if (!_client.IsInstalled)
        {
            return Task.FromResult(UpdateCheckOutcome.NotInstalled);
        }

        return RunSharedCheckAsync();
    }

    private async Task<UpdateCheckOutcome> RunSharedCheckAsync()
    {
        Task<UpdateCheckOutcome> task;
        lock (_gate)
        {
            task = _inFlight ??= CheckOnceAsync();
        }

        var outcome = await task.ConfigureAwait(false);

        lock (_gate)
        {
            if (ReferenceEquals(_inFlight, task))
            {
                _inFlight = null;
            }
        }

        return outcome;
    }

    private async Task<UpdateCheckOutcome> CheckOnceAsync()
    {
        try
        {
            var check = await _client.CheckAsync().ConfigureAwait(false);
            if (!check.Available)
            {
                return UpdateCheckOutcome.UpToDate;
            }

            if (!await _client.DownloadAsync().ConfigureAwait(false))
            {
                return UpdateCheckOutcome.Failed;
            }

            UpdateReady?.Invoke(check.Version ?? string.Empty);
            return UpdateCheckOutcome.Ready;
        }
        catch (Exception)
        {
            return UpdateCheckOutcome.Failed;
        }
    }

    public void Apply() => _client.ApplyAndRestart();
}
