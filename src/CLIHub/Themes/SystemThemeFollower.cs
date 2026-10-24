namespace CLIHub.Themes;

using System;
using System.Windows;

using Microsoft.Win32;

using Serilog;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Logging;
using CLIHub.Core.Models;
using CLIHub.Core.Windows;

/// <summary>
///   Follows the Windows apps-use-light-theme setting while the stored theme is "System" and
///   re-applies the matching dark or light palette when it changes during a running session.
/// </summary>
/// <remarks>
///   <para>
///     The guards (the stored theme read and the light/dark comparison) run on the dedicated
///     system-events thread and touch no UI; only the palette apply is marshaled to the UI thread
///     through the dispatch delegate supplied to <see cref="Start" />.
///   </para>
///   <para>
///     The preference-change category is deliberately ignored: the categories raised for the
///     apps light/dark toggle vary across Windows builds, so the registry-value comparison — not
///     the event category — is the filter. Unrelated preference events read the same value,
///     observe no flip, and dispatch nothing.
///   </para>
///   <para>
///     One <see cref="UserPreferenceChangedEventHandler" /> instance is kept for both the
///     subscription and its removal, because <c> SystemEvents.UserPreferenceChanged </c> is a
///     static event whose handlers must be detached when the owner is disposed.
///   </para>
/// </remarks>
public sealed class SystemThemeFollower : IDisposable
{
    private readonly PaletteSwitcher switcher;
    private readonly Func<string?> readStoredTheme;
    private readonly Func<bool> isSystemLight;
    private readonly Action<UserPreferenceChangedEventHandler> subscribe;
    private readonly Action<UserPreferenceChangedEventHandler> unsubscribe;
    private readonly UserPreferenceChangedEventHandler preferenceChangedHandler;

    private Action<Action>? dispatch;
    private bool lastKnownLight;
    private bool started;
    private bool disposed;

    /// <summary>
    ///   Creates a follower wired to the production collaborators: the shared palette switcher,
    ///   the stored theme read from config.json, the Windows apps light mode, and the
    ///   <c> SystemEvents.UserPreferenceChanged </c> broadcast.
    /// </summary>
    public SystemThemeFollower()
        : this(
            PaletteSwitcher.Shared,
            static () => PreferenceReader.ReadAppTheme(AppPaths.ConfigFile),
            SystemTheme.IsLight,
            static handler => SystemEvents.UserPreferenceChanged += handler,
            static handler => SystemEvents.UserPreferenceChanged -= handler)
    {
    }

    /// <summary>
    ///   Creates a follower with explicit collaborators. Test seam; production code uses the
    ///   parameterless constructor.
    /// </summary>
    /// <param name="switcher"> The switcher owning the palette overlay. </param>
    /// <param name="readStoredTheme"> Reads the stored theme token from the configuration. </param>
    /// <param name="isSystemLight"> Reads the Windows apps light mode. </param>
    /// <param name="subscribe"> Subscribes a handler to the preference-change broadcast. </param>
    /// <param name="unsubscribe"> Removes a handler from the preference-change broadcast. </param>
    internal SystemThemeFollower(PaletteSwitcher switcher,
                                 Func<string?> readStoredTheme,
                                 Func<bool> isSystemLight,
                                 Action<UserPreferenceChangedEventHandler> subscribe,
                                 Action<UserPreferenceChangedEventHandler> unsubscribe)
    {
        this.switcher = switcher ?? throw new ArgumentNullException(nameof(switcher));
        this.readStoredTheme = readStoredTheme ?? throw new ArgumentNullException(nameof(readStoredTheme));
        this.isSystemLight = isSystemLight ?? throw new ArgumentNullException(nameof(isSystemLight));
        this.subscribe = subscribe ?? throw new ArgumentNullException(nameof(subscribe));
        this.unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
        this.preferenceChangedHandler = this.OnPreferenceChanged;
    }

    /// <summary>
    ///   Records the UI-thread dispatch, captures the currently observed Windows mode, and
    ///   subscribes to the preference-change broadcast. The subscription is taken last, so no
    ///   handler can run before the state is initialized. A second call without an intervening
    ///   <see cref="Dispose" /> is a no-op.
    /// </summary>
    /// <param name="dispatch"> The delegate that runs the palette apply on the UI thread. </param>
    public void Start(Action<Action> dispatch)
    {
        ArgumentNullException.ThrowIfNull(dispatch);

        if (this.started)
        {
            return;
        }

        this.dispatch = dispatch;
        this.lastKnownLight = this.isSystemLight();
        this.started = true;

        this.subscribe(this.preferenceChangedHandler);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        this.unsubscribe(this.preferenceChangedHandler);
        this.started = false;
        this.dispatch = null;
    }

    /// <summary>
    ///   Decides, from one preference-change event, whether the palette must be re-applied: the
    ///   event is skipped when the follower is not started, when the stored theme is fixed, or
    ///   when the Windows mode did not flip since the last observation.
    /// </summary>
    /// <param name="sender"> Event source; ignored. </param>
    /// <param name="e"> Event arguments; the category is deliberately ignored. </param>
    private void OnPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (!this.started)
        {
            return;
        }

        if (AppThemes.Parse(this.readStoredTheme()) != AppTheme.System)
        {
            return;
        }

        var isLight = this.isSystemLight();
        if (isLight == this.lastKnownLight)
        {
            return;
        }

        this.lastKnownLight = isLight;

        if (this.dispatch is { } dispatch)
        {
            dispatch(() => this.Apply(AppTheme.System));
        }
    }

    /// <summary>
    ///   Re-applies the palette of the System theme through the shared switcher, mirroring the
    ///   Settings-save apply path. The Windows mode is re-read here so the applied palette
    ///   reflects the freshest value. A successful re-apply logs one informational line through
    ///   the static Serilog <c> Log </c> class (the same idiom <see cref="PaletteSwitcher" /> uses
    ///   for its failure warning); the guard paths and a failed apply log nothing here — the
    ///   switcher logs its own warning when a palette cannot load.
    /// </summary>
    /// <param name="theme"> The theme whose palette becomes active. </param>
    private void Apply(AppTheme theme)
    {
        if (Application.Current is not { } application)
        {
            return;
        }

        var isLight = this.isSystemLight();
        if (this.switcher.Apply(application.Resources.MergedDictionaries, theme, isLight))
        {
            Log.Information(
                "The Windows apps mode changed to {AppsMode}; the {Theme} palette was re-resolved and re-applied",
                isLight ? "light" : "dark",
                theme);
        }
    }
}
