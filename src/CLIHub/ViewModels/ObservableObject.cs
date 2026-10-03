namespace CLIHub.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
///   Minimal <see cref="INotifyPropertyChanged"/> base for view models.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///   Raises <see cref="PropertyChanged"/> for the given property.
    /// </summary>
    /// <param name="propertyName"> The property name; defaults to the caller. </param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    ///   Updates the backing field and raises <see cref="PropertyChanged"/> when the value changed.
    /// </summary>
    /// <typeparam name="T"> The property type. </typeparam>
    /// <param name="field"> The backing field, passed by reference. </param>
    /// <param name="value"> The new value. </param>
    /// <param name="propertyName"> The property name; defaults to the caller. </param>
    /// <returns> True when the value changed and notifications were raised. </returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
