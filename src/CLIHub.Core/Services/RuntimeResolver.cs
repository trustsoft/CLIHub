using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed class RuntimeResolver
{
    public const string DefaultRuntime = "cmd";

    public string Resolve(AgentActionSpec? action, string? configRuntime)
    {
        if (action is not null && !string.IsNullOrWhiteSpace(action.Runtime))
        {
            return Normalize(action.Runtime);
        }

        if (!string.IsNullOrWhiteSpace(configRuntime))
        {
            return Normalize(configRuntime);
        }

        return DefaultRuntime;
    }

    private static string Normalize(string runtime) => runtime.Trim().ToLowerInvariant();
}
