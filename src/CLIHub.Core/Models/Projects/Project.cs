namespace CLIHub.Core.Models;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
///   Represents a project directory tracked by CLIHub. Mutable display properties
///   raise property-change notifications so bound views update in place.
/// </summary>
public class Project : INotifyPropertyChanged
{
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
    ///   How the project entered the list: registered manually by the user,
    ///   or discovered by repository scanning. Defaults to <see cref="ProjectSource.Manual" />.
    /// </summary>
    public ProjectSource Source { get; set; } = ProjectSource.Manual;

    /// <summary>
    ///   Whether this project is marked as a favorite.
    /// </summary>
    public bool IsFavorite
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            this.OnPropertyChanged();
        }
    }

    /// <summary>
    ///   Timestamp of when this project was last used.
    /// </summary>
    public DateTime LastUsed
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            this.OnPropertyChanged();
        }
    }

    /// <summary>
    ///   Optional path to project-specific logo image.
    /// </summary>
    public string? LogoPath
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            this.OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
