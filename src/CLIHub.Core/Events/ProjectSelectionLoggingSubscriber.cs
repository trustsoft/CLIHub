namespace CLIHub.Core.Events;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.Logging;

/// <summary>
///   Logs project selection events with structured context.
/// </summary>
public sealed class ProjectSelectionLoggingSubscriber : IDisposable
{
    private readonly ILoggingContextProvider loggingContext;
    private readonly ILogger<ProjectSelectionLoggingSubscriber> logger;
    private readonly IDisposable subscription;

    /// <summary>
    ///   Initializes a new instance of the <see cref="ProjectSelectionLoggingSubscriber" /> class.
    /// </summary>
    /// <param name="subscriber"> The domain event subscriber. </param>
    /// <param name="loggingContext"> The logging context provider. </param>
    /// <param name="logger"> The logger. </param>
    public ProjectSelectionLoggingSubscriber(
        IDomainEventSubscriber subscriber,
        ILoggingContextProvider loggingContext,
        ILogger<ProjectSelectionLoggingSubscriber> logger)
    {
        ArgumentNullException.ThrowIfNull(subscriber);
        this.loggingContext = loggingContext ?? throw new ArgumentNullException(nameof(loggingContext));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this.subscription = subscriber.Subscribe<ProjectSelected>(this.OnProjectSelected);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.subscription.Dispose();
    }

    private void OnProjectSelected(ProjectSelected evt)
    {
        using (this.loggingContext.BeginOperation("ProjectSelected"))
        {
            this.logger.LogDebug("Project selected: {ProjectId} at {Path}", evt.Id, evt.Path);
        }
    }
}
