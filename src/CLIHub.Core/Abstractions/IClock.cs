namespace CLIHub.Core.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
