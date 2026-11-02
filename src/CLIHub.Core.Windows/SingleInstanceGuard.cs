namespace CLIHub.Core.Windows;

using System.IO.Pipes;
using System.Text;

/// <summary>
///   Ensures a single application instance. The first instance owns a named mutex and
///   listens on a named pipe; later instances signal it to activate and then exit.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable, ISingleInstanceGuard
{
    private const string MutexName = @"Local\CLIHub.SingleInstance";
    private const string PipeName = "CLIHub.SingleInstance";

    // Activation delivery: a monotonic ~2 s deadline, retry backoff starting at 50 ms and growing to
    // the longest single connection attempt.
    private const int ActivationDeadlineMs = 2000;
    private const int MaxConnectTimeoutMs = 500;
    private const int InitialRetryDelayMs = 50;
    private const int MaxRetryDelayMs = 500;

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
    ///   True when the guard has been disposed. Visible to test assemblies for lifecycle assertions.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///   Raised on the first instance when another instance signals activation.
    ///   Handlers are invoked on a background thread.
    /// </summary>
    public event Action? ActivationRequested;

    /// <summary>
    ///   Creates the guard with the production mutex and pipe names and acquires the
    ///   single-instance mutex.
    /// </summary>
    public SingleInstanceGuard() : this(MutexName, PipeName)
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

            NamedPipeServerStream? initialServer = null;
            try
            {
                // The pipe instance is listening as soon as it exists, so a duplicate that signals
                // right after this constructor returns is still delivered to the server loop.
                initialServer = CreateServerStream(this.pipeName);
            }
            catch
            {
                // Activation degrades but startup continues: the server loop retries creation
                // through the same error-handling path it uses for every other failure.
            }

            this.serverTask = Task.Run(() => this.ServerLoopAsync(this.cts.Token, initialServer));
        }
    }

    /// <summary>
    ///   Signals the running primary instance to show its window. Safe to call from a
    ///   second instance; failures are swallowed so the caller can exit quietly. The signal is
    ///   retried on a monotonic deadline, so a server that becomes ready shortly after this call
    ///   still receives it.
    /// </summary>
    public void SignalActivation()
    {
        var deadline = Environment.TickCount64 + ActivationDeadlineMs;
        var delayMs = InitialRetryDelayMs;

        while (Environment.TickCount64 < deadline)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", this.pipeName, PipeDirection.Out);

                // Keep one attempt, including its wait, inside the overall deadline.
                var remaining = (int)Math.Max(1, deadline - Environment.TickCount64);
                client.Connect(Math.Min(MaxConnectTimeoutMs, remaining));

                using var writer = new StreamWriter(client) { AutoFlush = true };
                writer.WriteLine("SHOW");
                return;
            }
            catch
            {
                var remaining = deadline - Environment.TickCount64;
                if (remaining <= 0)
                {
                    return; // Out of time; give up silently.
                }

                Thread.Sleep((int)Math.Min(delayMs, remaining));
                delayMs = Math.Min(delayMs * 2, MaxRetryDelayMs);
            }
        }
    }

    private static NamedPipeServerStream CreateServerStream(string pipeName)
        => new(
            pipeName,
            PipeDirection.In,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

    private async Task ServerLoopAsync(CancellationToken token, NamedPipeServerStream? initialServer)
    {
        var server = initialServer;

        while (!token.IsCancellationRequested)
        {
            try
            {
                server ??= CreateServerStream(this.pipeName);

                await server.WaitForConnectionAsync(token);

                string? message;
                using (var reader = new StreamReader(
                    server,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 1024,
                    leaveOpen: true))
                {
                    message = await reader.ReadLineAsync(token);
                }

                if (string.Equals(message, "SHOW", StringComparison.OrdinalIgnoreCase))
                {
                    this.ActivationRequested?.Invoke();
                }

                // The next iteration creates a fresh pipe instance so later duplicate launches are
                // served again; the client retry covers the turn between this disposal and the next
                // WaitForConnectionAsync.
                server.Dispose();
                server = null;
            }
            catch (OperationCanceledException)
            {
                server?.Dispose();
                server = null;
                break;
            }
            catch
            {
                server?.Dispose();
                server = null;

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

        // Covers the case where cancellation was already requested before the loop first ran, so
        // the stream created by the constructor is still owned by the loop when it never starts.
        server?.Dispose();
    }

    /// <summary>
    ///   Releases the mutex and signal resources.
    /// </summary>
    public void Dispose()
    {
        this.IsDisposed = true;

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
