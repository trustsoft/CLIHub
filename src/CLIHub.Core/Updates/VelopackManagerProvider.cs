namespace CLIHub.Core.Updates;

using Microsoft.Extensions.Logging;

using Velopack;
using Velopack.Sources;

/// <summary>
///   Manages Velopack UpdateManager lifecycle and initialization.
/// </summary>
internal sealed class VelopackManagerProvider
{
    private readonly ILogger logger;
    private readonly Lazy<UpdateManager?> manager;

    /// <summary>
    ///   Creates the provider with the given repository URL.
    /// </summary>
    /// <param name="repositoryUrl"> The update feed URL (GitHub or local). </param>
    /// <param name="logger"> Logger for initialization failures. </param>
    public VelopackManagerProvider(string repositoryUrl, ILogger logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.manager = new Lazy<UpdateManager?>(() => this.CreateManager(repositoryUrl));
    }

    /// <summary>
    ///   Test constructor: wraps an already-initialized manager.
    /// </summary>
    /// <param name="manager"> The test update manager. </param>
    public VelopackManagerProvider(UpdateManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        this.logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        this.manager = new Lazy<UpdateManager?>(() => manager);
    }

    /// <summary>
    ///   Gets the UpdateManager, or null if Velopack is not initialized.
    /// </summary>
    public UpdateManager? Manager => this.manager.Value;

    /// <summary>
    ///   Gets a value indicating whether a manager is available.
    /// </summary>
    public bool IsAvailable => this.Manager is not null;

    private UpdateManager? CreateManager(string repositoryUrl)
    {
        try
        {
            // A GitHub repository URL uses the GitHub Releases source; any other URL or a local
            // folder (useful for testing and self-hosted mirrors) uses Velopack's default source.
            if (repositoryUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            {
                return new UpdateManager(new GithubSource(repositoryUrl, null, false));
            }

            return new UpdateManager(repositoryUrl);
        }
        catch (Exception ex)
        {
            // Thrown when VelopackApp.Build().Run() has not initialized a locator
            // (for example, running under a test host).
            this.logger.LogDebug(ex, "Velopack is not initialized; update checks are disabled");
            return null;
        }
    }
}
