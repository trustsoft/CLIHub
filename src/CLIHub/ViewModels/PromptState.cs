namespace CLIHub.ViewModels;

/// <summary>
///   Tracks whether a modal prompt opened from the launch window is active, so that the window
///   does not hide itself when it loses focus to that prompt.
/// </summary>
public sealed class PromptState
{
    private int depth;

    /// <summary>
    ///   True while at least one prompt is open.
    /// </summary>
    public bool IsPromptOpen => this.depth > 0;

    /// <summary>
    ///   Marks a prompt as open until the returned scope is disposed.
    /// </summary>
    public IDisposable Begin()
    {
        this.depth++;
        return new Scope(this);
    }

    private void End() => this.depth = Math.Max(0, this.depth - 1);

    private sealed class Scope(PromptState owner) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            owner.End();
        }
    }
}
