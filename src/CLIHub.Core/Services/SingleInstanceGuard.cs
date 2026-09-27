namespace CLIHub.Core.Services;

using System.IO.Pipes;

/// <summary>
/// Ensures a single application instance. The first instance owns a named mutex and
/// listens on a named pipe; later instances signal it to activate and then exit.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private const string MutexName = @"Local\CLIHub.SingleInstance";
    private const string PipeName = "CLIHub.SingleInstance";

    private readonly Mutex _mutex;
    private readonly bool _isFirstInstance;
    private readonly CancellationTokenSource? _cts;
    private readonly Task? _serverTask;

    /// <summary>
    /// True when this process acquired the mutex and is the primary instance.
    /// </summary>
    public bool IsFirstInstance => _isFirstInstance;

    /// <summary>
    /// Raised on the first instance when another instance signals activation.
    /// Handlers are invoked on a background thread.
    /// </summary>
    public event Action? ActivationRequested;

    public SingleInstanceGuard()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        _isFirstInstance = createdNew;

        if (_isFirstInstance)
        {
            _cts = new CancellationTokenSource();
            _serverTask = Task.Run(() => ServerLoopAsync(_cts.Token));
        }
    }

    /// <summary>
    /// Signals the running primary instance to show its window. Safe to call from a
    /// second instance; failures are swallowed so the caller can exit quietly.
    /// </summary>
    public void SignalActivation()
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(500);
                using var writer = new StreamWriter(client) { AutoFlush = true };
                writer.WriteLine("SHOW");
                return;
            }
            catch
            {
                Thread.Sleep(100);
            }
        }
    }

    private async Task ServerLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(token);

                using var reader = new StreamReader(server);
                var message = await reader.ReadLineAsync(token);

                if (string.Equals(message, "SHOW", StringComparison.OrdinalIgnoreCase))
                {
                    ActivationRequested?.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                try
                {
                    await Task.Delay(200, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    public void Dispose()
    {
        try
        {
            _cts?.Cancel();
            _serverTask?.Wait(TimeSpan.FromMilliseconds(500));
        }
        catch
        {
            // best-effort shutdown
        }

        _cts?.Dispose();

        if (_isFirstInstance)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // not owned; ignore
            }
        }

        _mutex.Dispose();
    }
}
