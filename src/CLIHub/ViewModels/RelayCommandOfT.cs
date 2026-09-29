namespace CLIHub.ViewModels;

using System.Windows.Input;

/// <summary>
/// A basic <see cref="ICommand"/> that invokes a delegate with a typed parameter.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Func<T, bool>? _canExecute;

    public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) =>
        parameter is T typed && (_canExecute?.Invoke(typed) ?? true);

    public void Execute(object? parameter)
    {
        if (parameter is T typed)
        {
            _execute(typed);
        }
    }
}
