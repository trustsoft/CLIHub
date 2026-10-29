namespace CLIHub.Core.Processes;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates all process operations: launching, capturing output, and finding running agents.
/// </summary>
public class ProcessOrchestrator : IProcessOrchestrator
{
    private readonly IRuntimeResolver runtimeResolver;
    private readonly ICommandLineBuilder commandLineBuilder;
    private readonly IInteractiveProcessRunner interactiveRunner;
    private readonly IProcessOutputRunner outputRunner;
    private readonly IAgentProcessFinder processFinder;
    private readonly ILogger<ProcessOrchestrator> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessOrchestrator"/> class.
    /// </summary>
    /// <param name="runtimeResolver">The runtime resolver.</param>
    /// <param name="commandLineBuilder">The command line builder.</param>
    /// <param name="interactiveRunner">The interactive process runner.</param>
    /// <param name="outputRunner">The process output runner.</param>
    /// <param name="processFinder">The agent process finder.</param>
    /// <param name="logger">The logger.</param>
    public ProcessOrchestrator(
        IRuntimeResolver runtimeResolver,
        ICommandLineBuilder commandLineBuilder,
        IInteractiveProcessRunner interactiveRunner,
        IProcessOutputRunner outputRunner,
        IAgentProcessFinder processFinder,
        ILogger<ProcessOrchestrator> logger)
    {
        this.runtimeResolver = runtimeResolver;
        this.commandLineBuilder = commandLineBuilder;
        this.interactiveRunner = interactiveRunner;
        this.outputRunner = outputRunner;
        this.processFinder = processFinder;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request)
    {
        using var scope = this.logger.BeginScope("LaunchInteractive {Runtime} {Executable}",
            request.RuntimeKind, request.Command.Executable);

        try
        {
            // Resolve runtime
            var runtime = await this.runtimeResolver.ResolveAsync(
                request.RuntimeKind,
                request.CancellationToken);

            if (runtime is null)
            {
                this.logger.LogWarning("Runtime {RuntimeKind} is not available", request.RuntimeKind);
                return ProcessResult.Failure($"Runtime {request.RuntimeKind} not available");
            }

            // Build command line
            var (fileName, arguments) = this.commandLineBuilder.BuildInteractive(
                runtime,
                request.Command,
                request.WorkingDirectory);

            // Map RuntimeKind to RuntimeType
            var runtimeType = request.RuntimeKind switch
            {
                RuntimeKind.CommandPrompt => RuntimeType.Cmd,
                RuntimeKind.PowerShell or RuntimeKind.PowerShell7 => RuntimeType.PowerShell,
                RuntimeKind.GitBash => RuntimeType.Cmd,
                _ => RuntimeType.Cmd
            };

            var runtimeInfo = new RuntimeInfo
            {
                ExecutablePath = fileName,
                Type = runtimeType
            };

            // Launch
            var startTime = DateTime.UtcNow;
            await this.interactiveRunner.LaunchAsync(
                arguments,
                runtimeInfo,
                request.WorkingDirectory,
                request.CancellationToken);

            var duration = DateTime.UtcNow - startTime;
            this.logger.LogInformation("Launched successfully in {Duration}", duration);
            return ProcessResult.Success(0, duration: duration);
        }
        catch (OperationCanceledException)
        {
            this.logger.LogInformation("Launch was cancelled");
            return ProcessResult.Cancelled();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to launch {Command}", request.Command.Executable);
            return ProcessResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<ProcessResult> CaptureOutputAsync(LaunchRequest request)
    {
        using var scope = this.logger.BeginScope("CaptureOutput {Runtime} {Executable}",
            request.RuntimeKind, request.Command.Executable);

        try
        {
            // Resolve runtime
            var runtime = await this.runtimeResolver.ResolveAsync(
                request.RuntimeKind,
                request.CancellationToken);

            if (runtime is null)
            {
                this.logger.LogWarning("Runtime {RuntimeKind} is not available", request.RuntimeKind);
                return ProcessResult.Failure($"Runtime {request.RuntimeKind} not available");
            }

            // Build command line for captured execution
            var (fileName, arguments) = this.commandLineBuilder.BuildCaptured(
                request.Command.Executable,
                request.Command.Arguments);

            var runtimeInfo = new RuntimeInfo
            {
                ExecutablePath = fileName,
                Type = RuntimeType.Cmd  // Capture always uses cmd.exe
            };

            // Build full command line with working directory
            var fullCommandLine = $"/D \"{request.WorkingDirectory}\" {arguments}";

            // Capture with default timeout
            var startTime = DateTime.UtcNow;
            var result = await this.outputRunner.RunAsync(
                fullCommandLine,
                runtimeInfo,
                TimeSpan.FromSeconds(30),
                request.CancellationToken);

            var duration = DateTime.UtcNow - startTime;

            // Map Infrastructure.ProcessResult to domain ProcessResult
            if (result.TimedOut)
            {
                this.logger.LogWarning("Command timed out after {Duration}", duration);
                return ProcessResult.Timeout();
            }

            var trimmedOutput = result.StandardOutput?.Trim() ?? string.Empty;

            this.logger.LogInformation("Captured output, exit code {ExitCode}", result.ExitCode);
            return ProcessResult.Success(
                result.ExitCode,
                output: trimmedOutput,
                duration: duration);
        }
        catch (OperationCanceledException)
        {
            this.logger.LogInformation("Capture was cancelled");
            return ProcessResult.Cancelled();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to capture output from {Command}", request.Command.Executable);
            return ProcessResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(
        ProcessMatchCriteria criteria)
    {
        using var scope = this.logger.BeginScope("FindRunningAgents {PluginId}", criteria.Plugin.Id);

        var processes = await this.processFinder.FindAsync(criteria, CancellationToken.None);
        this.logger.LogInformation("Found {Count} running agent process(es)", processes.Count);
        return processes;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PluginAgentProcess>> FindRunningAgentsAsync(
        IReadOnlyList<Plugin> plugins,
        AgentProcessMatchDetail detail)
    {
        using var scope = this.logger.BeginScope("FindRunningAgents {PluginCount}", plugins.Count);

        var matches = await this.processFinder.FindAllAsync(plugins, detail, CancellationToken.None);
        this.logger.LogInformation("Found {Count} running agent process(es)", matches.Count);
        return matches;
    }

    /// <inheritdoc/>
    public async Task<RuntimeExecutable?> ResolveRuntimeAsync(
        RuntimeKind kind,
        CancellationToken cancellationToken = default)
    {
        return await this.runtimeResolver.ResolveAsync(kind, cancellationToken);
    }
}
