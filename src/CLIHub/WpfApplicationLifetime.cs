namespace CLIHub;

using System.Windows;

/// <summary>
///   WPF implementation of the application lifetime boundary.
/// </summary>
public sealed class WpfApplicationLifetime : IApplicationLifetime
{
    /// <inheritdoc />
    public void Shutdown() => Application.Current.Shutdown();
}
