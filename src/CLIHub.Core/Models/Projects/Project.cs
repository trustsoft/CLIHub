namespace CLIHub.Core.Models;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
///   Represents a project directory tracked by CLIHub. Mutable display properties raise
///   property-change notifications so bound views update in place.
/// </summary>
public class Project : INotifyPropertyChanged
{
    private bool isFavorite;
    private DateTime lastUsed;
    private string? logoPath;

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
    ///   How the project entered the list: registered manually by the user, or discovered by
    ///   repository scanning. Defaults to <see cref="ProjectSource.Manual" />.
    /// </summary>
    public ProjectSource Source { get; set; } = ProjectSource.Manual;

    /// <summary>
    ///   Whether this project is marked as a favorite.
    /// </summary>
    public bool IsFavorite
    {
        get => this.isFavorite;
        set
        {
            if (this.isFavorite == value)
            {
                return;
            }

            this.isFavorite = value;
            this.OnPropertyChanged();
        }
    }

    /// <summary>
    ///   Timestamp of when this project was last used.
    /// </summary>
    public DateTime LastUsed
    {
        get => this.lastUsed;
        set
        {
            if (this.lastUsed == value)
            {
                return;
            }

            this.lastUsed = value;
            this.OnPropertyChanged();
        }
    }

    /// <summary>
    ///   Optional path to project-specific logo image.
    /// </summary>
    public string? LogoPath
    {
        get => this.logoPath;
        set
        {
            if (this.logoPath == value)
            {
                return;
            }

            this.logoPath = value;
            this.OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
