namespace CLIHub.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;

/// <summary>
///   Enriched display row for a project, shared by every project surface: the record's
///   identity plus the enriched snapshot (final logo path, folder availability, favorite
///   flag) and derived display fields. Raises property-change notifications so bound views
///   update in place; the catalog applies fresh snapshots through <see cref="Update" />.
///   Carries state only — surface view models own the commands.
/// </summary>
public sealed class ProjectModel : INotifyPropertyChanged
{
    private string name = string.Empty;
    private string path = string.Empty;
    private string? logoPath;
    private bool isFolderAvailable;
    private bool isFavorite;

    /// <summary>
    ///   Creates the row from an enriched snapshot.
    /// </summary>
    /// <param name="snapshot"> The enriched snapshot to display. </param>
    public ProjectModel(EnrichedProject snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        this.Id = snapshot.Project.Id;
        this.Source = snapshot.Project.Source;
        this.Apply(snapshot);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///   The stable project identifier; the row's identity across refreshes and surfaces.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///   How the project entered the list. Immutable once created: the service never
    ///   reclassifies a project's provenance.
    /// </summary>
    public ProjectSource Source { get; }

    /// <summary>
    ///   The project display name.
    /// </summary>
    public string Name
    {
        get => this.name;
        private set => this.SetValue(ref this.name, value);
    }

    /// <summary>
    ///   The full project folder path.
    /// </summary>
    public string Path
    {
        get => this.path;
        private set => this.SetValue(ref this.path, value);
    }

    /// <summary>
    ///   The final logo path — the project logo resolver has already applied the default-logo
    ///   fallback — rendered through the shared <c>PathToImage</c> converter.
    /// </summary>
    public string? LogoPath
    {
        get => this.logoPath;
        private set => this.SetValue(ref this.logoPath, value);
    }

    /// <summary>
    ///   Whether the project folder existed when the last snapshot was taken.
    /// </summary>
    public bool IsFolderAvailable
    {
        get => this.isFolderAvailable;
        private set
        {
            if (this.SetValue(ref this.isFolderAvailable, value))
            {
                this.OnPropertyChanged(nameof(this.CanOpen));
            }
        }
    }

    /// <summary>
    ///   Whether the project is marked as a favorite.
    /// </summary>
    public bool IsFavorite
    {
        get => this.isFavorite;
        private set => this.SetValue(ref this.isFavorite, value);
    }

    /// <summary>
    ///   Whether the project was registered manually rather than discovered by scanning.
    /// </summary>
    public bool IsManual => this.Source == ProjectSource.Manual;

    /// <summary>
    ///   Whether the surface may offer the remove action: manual provenance only, since
    ///   discovered projects are managed by Project Discovery.
    /// </summary>
    public bool CanRemove => this.IsManual;

    /// <summary>
    ///   Whether the project folder can be opened: it existed in the last snapshot.
    /// </summary>
    public bool CanOpen => this.isFolderAvailable;

    /// <summary>
    ///   The single-letter provenance mark shown on a row: "M" for a manual project and
    ///   "D" for a discovered project.
    /// </summary>
    public string SourceMark => this.IsManual ? "M" : "D";

    /// <summary>
    ///   The full provenance label, used as the mark's tooltip and accessible name.
    /// </summary>
    public string SourceLabel => this.IsManual ? "Manual" : "Discovered";

    /// <summary>
    ///   The full folder path for surfaces that show it untrimmed (registry rows and tooltips);
    ///   the launch window keeps its width-aware trimming over <see cref="Path" />.
    /// </summary>
    public string DisplayPath => this.Path;

    /// <summary>
    ///   Applies a fresh enriched snapshot in place, raising notifications only for values
    ///   that changed.
    /// </summary>
    /// <param name="snapshot"> The enriched snapshot for this row's project. </param>
    public void Update(EnrichedProject snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!string.Equals(snapshot.Project.Id, this.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException("The snapshot belongs to a different project.", nameof(snapshot));
        }

        this.Apply(snapshot);
    }

    private void Apply(EnrichedProject snapshot)
    {
        this.Name = snapshot.Project.Name;
        this.Path = snapshot.Project.Path;
        this.LogoPath = snapshot.LogoPath;
        this.IsFolderAvailable = snapshot.FolderExists;
        this.IsFavorite = snapshot.Project.IsFavorite;
    }

    private bool SetValue<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        this.OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
