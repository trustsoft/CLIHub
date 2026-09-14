using CLIHub.Core.Abstractions;

namespace CLIHub.Core.Services;

public sealed class UpdateService
{
    private readonly IUpdateClient _client;
    private readonly ConfigStore _store;

    public UpdateService(IUpdateClient client, ConfigStore store)
    {
        _client = client;
        _store = store;
    }

    public event Action<string>? UpdateReady;

    public async Task<bool> RunStartupCheckAsync()
    {
        try
        {
            if (!_client.IsInstalled || !_store.Load().Update.EffectiveCheckOnStartup)
            {
                return false;
            }

            var check = await _client.CheckAsync();
            if (!check.Available)
            {
                return false;
            }

            if (!await _client.DownloadAsync())
            {
                return false;
            }

            UpdateReady?.Invoke(check.Version ?? string.Empty);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Apply() => _client.ApplyAndRestart();
}
