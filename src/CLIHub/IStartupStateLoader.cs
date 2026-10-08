namespace CLIHub;

/// <summary>
///   Loads required startup state before the application session begins.
/// </summary>
public interface IStartupStateLoader
{
    /// <summary>
    ///   Initializes plugins, loads user preferences, applies startup preferences,
    ///   and returns the immutable startup state.
    /// </summary>
    /// <returns> The loaded startup state. </returns>
    StartupState Load();
}
