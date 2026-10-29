namespace CLIHub.Core.Events;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.Logging;

/// <summary>
///   Emits structured audit log for agent launches.
/// </summary>
public sealed class AgentLaunchAuditSubscriber : IDisposable
{
    private readonly ILoggingContextProvider loggingContext;
    private readonly ILogger<AgentLaunchAuditSubscriber> logger;
    private readonly IDisposable subscription;

    /// <summary>
    ///   Initializes a new instance of the <see cref="AgentLaunchAuditSubscriber" /> class.
    /// </summary>
    /// <param name="subscriber"> The domain event subscriber. </param>
    /// <param name="loggingContext"> The logging context provider. </param>
    /// <param name="logger"> The logger. </param>
    public AgentLaunchAuditSubscriber(
        IDomainEventSubscriber subscriber,
        ILoggingContextProvider loggingContext,
        ILogger<AgentLaunchAuditSubscriber> logger)
    {
        ArgumentNullException.ThrowIfNull(subscriber);
        this.loggingContext = loggingContext ?? throw new ArgumentNullException(nameof(loggingContext));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this.subscription = subscriber.Subscribe<AgentLaunched>(this.OnAgentLaunched);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.subscription.Dispose();
    }

    private void OnAgentLaunched(AgentLaunched evt)
    {
        using (this.loggingContext.BeginOperation("AgentLaunch"))
        {
            this.logger.LogDebug(
                "Agent launched: {PluginId} {CommandKind} in {ProjectPath}",
                evt.PluginId,
                evt.CommandKind,
                evt.ProjectPath ?? "<no-project>");
        }
    }
}
