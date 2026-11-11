namespace CLIHub.ViewModels;

using System.Windows.Input;

/// <summary>
///   Availability plumbing shared by the basic commands: the explicit subscriber list and the
///   CommandManager re-query wiring behind <see cref="CanExecuteChanged" />.
/// </summary>
public abstract class RelayCommandBase : ICommand
{
    private EventHandler? explicitCanExecuteChanged;

    /// <summary>
    ///   Raised when <see cref="CanExecute" /> may have changed. Subscribers receive both the
    ///   CommandManager's batch re-queries and explicit raises through
    ///   <see cref="RaiseCanExecuteChanged" />, so availability changes that no input event
    ///   observes still reach the UI.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add
        {
            this.explicitCanExecuteChanged += value;
            CommandManager.RequerySuggested += value;
        }

        remove
        {
            this.explicitCanExecuteChanged -= value;
            CommandManager.RequerySuggested -= value;
        }
    }

    /// <summary>
    ///   Raises <see cref="CanExecuteChanged" /> for the explicit subscribers, forcing elements
    ///   that cache the <see cref="CanExecute" /> result to re-query it now.
    /// </summary>
    public void RaiseCanExecuteChanged() => this.explicitCanExecuteChanged?.Invoke(this, EventArgs.Empty);

    /// <inheritdoc />
    public abstract bool CanExecute(object? parameter);

    /// <inheritdoc />
    public abstract void Execute(object? parameter);
}
