namespace CLIHub.ViewModels;

using System.Windows.Input;

/// <summary>
///   A basic <see cref="ICommand" /> that invokes a delegate.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action execute;
    private readonly Func<bool>? canExecute;
    private EventHandler? explicitCanExecuteChanged;

    /// <summary>
    ///   Creates the command.
    /// </summary>
    /// <param name="execute"> The action to invoke. </param>
    /// <param name="canExecute"> Optional availability predicate. </param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

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
    public bool CanExecute(object? parameter) => this.canExecute?.Invoke() ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => this.execute();
}
