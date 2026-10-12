namespace CLIHub.ViewModels;

using System.Windows.Input;

/// <summary>
///   A basic <see cref="ICommand"/> that invokes a delegate with a typed parameter.
/// </summary>
/// <typeparam name="T"> The type of the command parameter. </typeparam>
public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T> execute;
    private readonly Func<T, bool>? canExecute;

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

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

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
