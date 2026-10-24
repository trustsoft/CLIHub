namespace CLIHub.Core.Windows.Tests;

internal sealed class FakeProcessCommandLineSource : IProcessCommandLineSource
{
    public Dictionary<int, string> CommandLines { get; } = [];

    public int CallCount { get; private set; }

    public Exception? Throw { get; set; }

    public ManualResetEventSlim? Block { get; set; }

    public IReadOnlyDictionary<int, string> GetCommandLines(IReadOnlySet<string> processNames)
    {
        this.CallCount++;

        if (this.Throw is not null)
        {
            throw this.Throw;
        }

        this.Block?.Wait();
        return this.CommandLines;
    }
}
