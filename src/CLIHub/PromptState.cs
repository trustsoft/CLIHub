namespace CLIHub;

/// <summary>
/// Tracks whether a modal prompt opened from the launch window is active, so that the window
/// does not hide itself when it loses focus to that prompt.
/// </summary>
public sealed class PromptState
{
    private int _depth;

    /// <summary>
    /// True while at least one prompt is open.
    /// </summary>
    public bool IsPromptOpen => _depth > 0;

    /// <summary>
    /// Marks a prompt as open until the returned scope is disposed.
    /// </summary>
    public IDisposable Begin()
    {
        _depth++;
        return new Scope(this);
    }

    private void End() => _depth = Math.Max(0, _depth - 1);

    private sealed class Scope(PromptState owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            owner.End();
        }
    }
}
