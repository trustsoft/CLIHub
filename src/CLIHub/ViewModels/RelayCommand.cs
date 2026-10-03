namespace CLIHub.ViewModels;

using System.Windows.Input;

/// <summary>
///   A basic <see cref="ICommand"/> that invokes a delegate.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    ///   Creates the command.
    /// </summary>
    /// <param name="execute"> The action to invoke. </param>
    /// <param name="canExecute"> Optional availability predicate. </param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute();
}
