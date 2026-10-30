namespace CLIHub.Tests.Support;

using CLIHub.Lifetime;

/// <summary>
/// Shared test infrastructure helpers.
/// </summary>
internal static class TestHelpers
{
    private static readonly Lazy<string> RepositoryRoot = new(() =>
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CLIHub.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    });

    /// <summary>
    /// Runs the specified action on a thread configured with STA apartment state.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="message">The message to include in the exception if the action fails.</param>
    internal static void RunOnSta(Action action, string message = "STA test failed.")
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw new Xunit.Sdk.XunitException(message, exception);
        }
    }

    /// <summary>
    /// Finds the repository root by walking up from the test assembly location.
    /// </summary>
    /// <returns>The repository root directory path.</returns>
    internal static string FindRepositoryRoot() => RepositoryRoot.Value;

    /// <summary>
    /// An <see cref="IApplicationOperationLifetime"/> that executes operations synchronously,
    /// blocking until completion.
    /// </summary>
    internal sealed class InlineOperationLifetime : IApplicationOperationLifetime
    {
        public CancellationToken Token => CancellationToken.None;

        public Task RunAsync(string operationName, Func<CancellationToken, Task> operation)
        {
            var task = operation(CancellationToken.None);
            task.GetAwaiter().GetResult();
            return task;
        }

        public Task StopAsync(TimeSpan timeout) => Task.CompletedTask;
    }
}
