namespace CLIHub.Core.Models;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
///   Represents a project directory tracked by CLIHub. Mutable display properties raise
///   property-change notifications so bound views update in place.
/// </summary>
public class Project : INotifyPropertyChanged
{
    private bool _isFavorite;
    private DateTime _lastUsed;
    private string? _logoPath;

    /// <summary>
    ///   Unique identifier for the project.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    ///   Display name of the project.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///   Full path to the project directory.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    ///   Whether this project is marked as a favorite.
    /// </summary>
    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (_isFavorite == value)
            {
                return;
            }

            _isFavorite = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///   Timestamp of when this project was last used.
    /// </summary>
    public DateTime LastUsed
    {
        get => _lastUsed;
        set
        {
            if (_lastUsed == value)
            {
                return;
            }

            _lastUsed = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///   Optional path to project-specific logo image.
    /// </summary>
    public string? LogoPath
    {
        get => _logoPath;
        set
        {
            if (_logoPath == value)
            {
                return;
            }

            _logoPath = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
