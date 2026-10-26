namespace CLIHub.ViewModels;

using System.Windows.Input;

/// <summary>
///   A basic <see cref="ICommand" /> that invokes a delegate with a typed parameter.
/// </summary>
/// <typeparam name="T"> The type of the command parameter. </typeparam>
public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T> execute;
    private readonly Func<T, bool>? canExecute;
    private EventHandler? explicitCanExecuteChanged;

    /// <summary>
    ///   Creates the command.
    /// </summary>
    /// <param name="execute"> The action to invoke with the typed parameter. </param>
    /// <param name="canExecute"> Optional availability predicate. </param>
    public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
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
    public bool CanExecute(object? parameter) =>
        parameter is T typed && (this.canExecute?.Invoke(typed) ?? true);

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (parameter is T typed)
        {
            this.execute(typed);
        }
    }
}
