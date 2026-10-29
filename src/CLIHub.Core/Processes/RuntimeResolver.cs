namespace CLIHub.Core.Processes;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
/// Adapts <see cref="IRuntimeExecutableLocator"/> to provide runtime resolution.
/// </summary>
public sealed class RuntimeResolver : IRuntimeResolver
{
    private readonly IRuntimeExecutableLocator locator;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeResolver"/> class.
    /// </summary>
    /// <param name="locator">The runtime executable locator.</param>
    public RuntimeResolver(IRuntimeExecutableLocator locator)
    {
        this.locator = locator;
    }

    /// <inheritdoc/>
    public Task<RuntimeExecutable?> ResolveAsync(
        RuntimeKind kind,
        CancellationToken cancellationToken = default)
    {
        var path = this.locator.Locate(kind);
        
        // Handle Windows built-in shells - locator returns null but they exist
        if (path is null && kind is RuntimeKind.PowerShell or RuntimeKind.CommandPrompt)
        {
            path = kind == RuntimeKind.PowerShell ? "powershell.exe" : "cmd.exe";
        }

        if (path is null)
        {
            return Task.FromResult<RuntimeExecutable?>(null);
        }

        var runtime = RuntimeExecutable.Create(kind, path, version: null);
        return Task.FromResult<RuntimeExecutable?>(runtime);
    }
}
