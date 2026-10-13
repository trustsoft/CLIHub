namespace CLIHub.Core.Infrastructure.Windows;

using System.IO.Pipes;

/// <summary>
///   Ensures a single application instance. The first instance owns a named mutex and
///   listens on a named pipe; later instances signal it to activate and then exit.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable, ISingleInstanceGuard
{
    private const string MutexName = @"Local\CLIHub.SingleInstance";
    private const string PipeName = "CLIHub.SingleInstance";

    private readonly Mutex mutex;
    private readonly string pipeName;
    private readonly bool isFirstInstance;
    private readonly CancellationTokenSource? cts;
    private readonly Task? serverTask;

    /// <summary>
    ///   True when this process acquired the mutex and is the primary instance.
    /// </summary>
    public bool IsFirstInstance => this.isFirstInstance;

    /// <summary>
    ///   Raised on the first instance when another instance signals activation.
    ///   Handlers are invoked on a background thread.
    /// </summary>
    public event Action? ActivationRequested;

    /// <summary>
    ///   Creates the guard with the production mutex and pipe names and acquires the
    ///   single-instance mutex.
    /// </summary>
    public SingleInstanceGuard()
        : this(MutexName, PipeName)
    {
    }

    /// <summary>
    ///   Creates the guard with explicit mutex and pipe names and acquires the mutex.
    ///   Tests use unique names so parallel test processes cannot observe each other's guards.
    /// </summary>
    /// <param name="mutexName"> The name of the single-instance mutex. </param>
    /// <param name="pipeName"> The name of the activation pipe. </param>
    public SingleInstanceGuard(string mutexName, string pipeName)
    {
        ArgumentNullException.ThrowIfNull(mutexName);
        ArgumentNullException.ThrowIfNull(pipeName);

        this.pipeName = pipeName;
        this.mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
        this.isFirstInstance = createdNew;

        if (this.isFirstInstance)
        {
            this.cts = new CancellationTokenSource();
            this.serverTask = Task.Run(() => this.ServerLoopAsync(this.cts.Token));
        }
    }

    /// <summary>
    ///   Signals the running primary instance to show its window. Safe to call from a
    ///   second instance; failures are swallowed so the caller can exit quietly.
    /// </summary>
    public void SignalActivation()
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", this.pipeName, PipeDirection.Out);
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
                    this.pipeName,
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

    /// <summary>
    ///   Releases the mutex and signal resources.
    /// </summary>
    public void Dispose()
    {
        try
        {
            this.cts?.Cancel();
            this.serverTask?.Wait(TimeSpan.FromMilliseconds(500));
        }
        catch
        {
            // best-effort shutdown
        }

        this.cts?.Dispose();

        if (this.isFirstInstance)
        {
            try
            {
                this.mutex.ReleaseMutex();
            }
            catch
            {
                // not owned; ignore
            }
        }

        this.mutex.Dispose();
    }
}
