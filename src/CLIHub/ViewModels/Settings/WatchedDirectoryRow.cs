namespace CLIHub.ViewModels;

using CLIHub.Core.Models;

/// <summary>
///   One watched-directory row: its path, the per-directory scan settings, and the number of
///   repositories found beneath it.
/// </summary>
public sealed class WatchedDirectoryRow : ObservableObject
{
    private ScanDepthOption selectedDepth;
    private DiscoveryModeOption selectedDiscovery;
    private int discoveredCount;

    /// <summary>
    ///   Creates a row.
    /// </summary>
    /// <param name="path"> The normalized watched root path. </param>
    /// <param name="depthOptions"> The allowed scan depths. </param>
    /// <param name="discoveryOptions"> The allowed discovery modes. </param>
    /// <param name="selectedDepth"> The row's scan depth. </param>
    /// <param name="selectedDiscovery"> The row's discovery mode. </param>
    public WatchedDirectoryRow(string path,
                               IReadOnlyList<ScanDepthOption> depthOptions,
                               IReadOnlyList<DiscoveryModeOption> discoveryOptions,
                               ScanDepthOption selectedDepth,
                               DiscoveryModeOption selectedDiscovery)
    {
        this.Path = path;
        this.DepthOptions = depthOptions;
        this.DiscoveryOptions = discoveryOptions;
        this.selectedDepth = selectedDepth;
        this.selectedDiscovery = selectedDiscovery;
    }

    /// <summary>
    ///   The normalized watched root path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///   The scan depths offered by the depth chip.
    /// </summary>
    public IReadOnlyList<ScanDepthOption> DepthOptions { get; }

    /// <summary>
    ///   The discovery modes offered by the discovery chip.
    /// </summary>
    public IReadOnlyList<DiscoveryModeOption> DiscoveryOptions { get; }

    /// <summary>
    ///   The row's scan depth.
    /// </summary>
    public ScanDepthOption SelectedDepth
    {
        get => this.selectedDepth;
        set
        {
            if (value is not null && this.SetProperty(ref this.selectedDepth, value))
            {
                this.OnPropertyChanged(nameof(this.DepthChipLabel));
            }
        }
    }

    /// <summary>
    ///   The row's discovery mode.
    /// </summary>
    public DiscoveryModeOption SelectedDiscovery
    {
        get => this.selectedDiscovery;
        set
        {
            if (value is not null && this.SetProperty(ref this.selectedDiscovery, value))
            {
                this.OnPropertyChanged(nameof(this.DiscoveryChipLabel));
            }
        }
    }

    /// <summary>
    ///   The text shown on the depth chip.
    /// </summary>
    public string DepthChipLabel => $"Depth {this.selectedDepth.Value}";

    /// <summary>
    ///   The text shown on the discovery chip.
    /// </summary>
    public string DiscoveryChipLabel => this.selectedDiscovery.Name;

    /// <summary>
    ///   The number of discovered repositories beneath this root.
    /// </summary>
    public int DiscoveredCount
    {
        get => this.discoveredCount;
        set
        {
            if (this.SetProperty(ref this.discoveredCount, value))
            {
                this.OnPropertyChanged(nameof(this.DiscoveredSummary));
            }
        }
    }

    /// <summary>
    ///   The repository count in singular/plural wording.
    /// </summary>
    public string DiscoveredSummary => this.discoveredCount == 1 ? "1 repository" : $"{this.discoveredCount} repositories";
}
