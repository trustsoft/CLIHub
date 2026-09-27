namespace CLIHub.Core.Models;

/// <summary>
/// Result of capturing a process's output.
/// </summary>
/// <param name="Started">Whether the process started.</param>
/// <param name="ExitCode">The process exit code, or -1 when it did not run to completion.</param>
/// <param name="StdOut">Captured standard output (trimmed).</param>
/// <param name="StdErr">Captured standard error (trimmed).</param>
public sealed record ProcessCaptureResult(bool Started, int ExitCode, string StdOut, string StdErr);
