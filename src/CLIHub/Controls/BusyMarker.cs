namespace CLIHub.Controls;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Threading;

using CLIHub.Core.UI;

/// <summary>
///   The visual tone of a <see cref="BusyMarker" />: the animated braille frames, a static
///   success glyph, or a static failure glyph.
/// </summary>
public enum BusyMarkerTone
{
    /// <summary>
    ///   The animated braille busy state, rendered in the primary text tone.
    /// </summary>
    Busy,

    /// <summary>
    ///   The static success glyph, rendered in the success tone.
    /// </summary>
    Success,

    /// <summary>
    ///   The static failure glyph, rendered in the danger tone.
    /// </summary>
    Failure
}

/// <summary>
///   An unboxed busy-state glyph marker: a fixed-width slot that cycles braille frames while an
///   operation runs, shows a static full-cell glyph when the host disables client-area
///   animations (reduced motion), and can show static success/failure glyphs for resolved tones.
///   The glyph is decorative — the adjacent text label carries the meaning — so it is excluded
///   from the automation tree, and the frame timer runs only while the marker is busy and
///   visible.
/// </summary>
/// <remarks>
///   The frame sequence, speed clamp, and reduced-motion glyph come from
///   <see cref="BusyGlyphSequence" /> in CLIHub.Core, keeping this control limited to WPF
///   specifics (dependency properties, timer lifecycle, template).
/// </remarks>
public sealed class BusyMarker : Control
{
    /// <summary>
    ///   Identifies the <see cref="IsBusy" /> property.
    /// </summary>
    public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
        nameof(IsBusy), typeof(bool), typeof(BusyMarker), new PropertyMetadata(false, OnIsBusyChanged));

    /// <summary>
    ///   Identifies the <see cref="Speed" /> property.
    /// </summary>
    public static readonly DependencyProperty SpeedProperty = DependencyProperty.Register(
        nameof(Speed), typeof(int), typeof(BusyMarker), new PropertyMetadata(80, OnSpeedChanged));

    /// <summary>
    ///   Identifies the <see cref="MarkerTone" /> property.
    /// </summary>
    public static readonly DependencyProperty MarkerToneProperty = DependencyProperty.Register(
        nameof(MarkerTone), typeof(BusyMarkerTone), typeof(BusyMarker), new PropertyMetadata(BusyMarkerTone.Busy));

    /// <summary>
    ///   Identifies the <see cref="CurrentFrame" /> property.
    /// </summary>
    public static readonly DependencyProperty CurrentFrameProperty = DependencyProperty.Register(
        nameof(CurrentFrame), typeof(string), typeof(BusyMarker), new PropertyMetadata(string.Empty));

    /// <summary>
    ///   Identifies the <see cref="ReducedMotionGlyph" /> property.
    /// </summary>
    public static readonly DependencyProperty ReducedMotionGlyphProperty = DependencyProperty.Register(
        nameof(ReducedMotionGlyph), typeof(string), typeof(BusyMarker), new PropertyMetadata(string.Empty));

    private DispatcherTimer? timer;
    private int currentFrameIndex;

    static BusyMarker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BusyMarker),
            new FrameworkPropertyMetadata(typeof(BusyMarker)));
    }

    /// <summary>
    ///   Gets or sets a value indicating whether the marker is animating. The frame timer runs
    ///   only while this is true and the marker is visible.
    /// </summary>
    public bool IsBusy
    {
        get => (bool)this.GetValue(IsBusyProperty);
        set => this.SetValue(IsBusyProperty, value);
    }

    /// <summary>
    ///   The frame duration in milliseconds: 80 by default, clamped to 40-200 on use so the
    ///   animation can neither flicker nor appear frozen.
    /// </summary>
    public int Speed
    {
        get => (int)this.GetValue(SpeedProperty);
        set => this.SetValue(SpeedProperty, value);
    }

    /// <summary>
    ///   The visual tone: the animated braille frames, a static success glyph, or a static
    ///   failure glyph.
    /// </summary>
    public BusyMarkerTone MarkerTone
    {
        get => (BusyMarkerTone)this.GetValue(MarkerToneProperty);
        set => this.SetValue(MarkerToneProperty, value);
    }

    /// <summary>
    ///   The glyph the template's slot currently displays: an animation frame, the
    ///   reduced-motion glyph, or empty when the marker is not busy.
    /// </summary>
    public string CurrentFrame
    {
        get => (string)this.GetValue(CurrentFrameProperty);
        private set => this.SetValue(CurrentFrameProperty, value);
    }

    /// <summary>
    ///   The static full-cell glyph recorded when reduced motion is detected; empty while the
    ///   animation runs. The template renders the glyph through <see cref="CurrentFrame" />.
    /// </summary>
    public string ReducedMotionGlyph
    {
        get => (string)this.GetValue(ReducedMotionGlyphProperty);
        private set => this.SetValue(ReducedMotionGlyphProperty, value);
    }

    /// <summary>
    ///   Subscribes to visibility changes so the timer can pause while the marker is collapsed
    ///   or out of the visual tree, then applies the current busy/visibility state.
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // The template can be re-applied (for example on a theme change); re-subscribing
        // idempotently keeps a single handler while refreshing the lifecycle state.
        this.IsVisibleChanged -= this.OnIsVisibleChanged;
        this.IsVisibleChanged += this.OnIsVisibleChanged;
        this.UpdateTimerState();
    }

    /// <summary>
    ///   Exposes the marker to UI Automation through a peer that removes it from the automation
    ///   tree: the glyph is decorative and the adjacent text label carries the meaning, so
    ///   screen readers must not announce the marker or its per-frame changes.
    /// </summary>
    /// <returns> A <see cref="BusyMarkerAutomationPeer" /> for this marker. </returns>
    protected override AutomationPeer OnCreateAutomationPeer() => new BusyMarkerAutomationPeer(this);

    /// <summary>
    ///   Keeps a <see cref="BusyMarker" /> and its glyph out of the UI Automation tree. The
    ///   marker is a decorative progress cue next to a text label that already carries the
    ///   meaning; exposing it would make screen readers announce an element (and, per frame,
    ///   its changing text) that conveys no information of its own.
    /// </summary>
    private sealed class BusyMarkerAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="BusyMarkerAutomationPeer" /> class.
        /// </summary>
        /// <param name="owner"> The busy marker this peer hides from automation. </param>
        public BusyMarkerAutomationPeer(BusyMarker owner)
            : base(owner)
        {
        }

        /// <summary> Removes the marker from the UI Automation control view. </summary>
        /// <returns>
        ///   <see langword="false" />, so structural navigation and screen readers skip the
        ///   decorative marker.
        /// </returns>
        protected override bool IsControlElementCore() => false;

        /// <summary> Removes the marker from the UI Automation content view. </summary>
        /// <returns>
        ///   <see langword="false" />, so content-only navigation never reaches the marker.
        /// </returns>
        protected override bool IsContentElementCore() => false;

        /// <summary> Exposes no children, so the glyph slot is never announced. </summary>
        /// <returns> <see langword="null" />, regardless of the template's glyph TextBlock. </returns>
        protected override List<AutomationPeer>? GetChildrenCore() => null;
    }

    private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((BusyMarker)d).UpdateTimerState();

    private static void OnSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((BusyMarker)d).UpdateTimerState();

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        this.UpdateTimerState();

    /// <summary>
    ///   Starts the frame timer while the marker is busy and visible, shows the static
    ///   reduced-motion glyph instead when client-area animations are disabled, and stops the
    ///   timer in every other state.
    /// </summary>
    private void UpdateTimerState()
    {
        if (!this.IsBusy || !this.IsVisible)
        {
            this.StopTimer();
            return;
        }

        if (!SystemParameters.ClientAreaAnimation)
        {
            this.ShowReducedMotionGlyph();
            return;
        }

        this.ReducedMotionGlyph = string.Empty;
        this.StartTimer();
    }

    private void StartTimer()
    {
        var interval = TimeSpan.FromMilliseconds(BusyGlyphSequence.ClampSpeed(this.Speed));

        if (this.timer is null)
        {
            this.timer = new DispatcherTimer
            {
                Interval = interval,
            };
            this.timer.Tick += this.OnTimerTick;
        }
        else
        {
            this.timer.Interval = interval;
        }

        this.timer.Start();
    }

    private void StopTimer()
    {
        this.timer?.Stop();
        this.timer = null;
    }

    private void ShowReducedMotionGlyph()
    {
        this.ReducedMotionGlyph = BusyGlyphSequence.GetReducedMotionGlyph();
        this.CurrentFrame = this.ReducedMotionGlyph;
        this.StopTimer();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            // The Windows animation setting changed while the timer ran; settle to the static
            // glyph and stop cycling.
            this.ShowReducedMotionGlyph();
            return;
        }

        this.currentFrameIndex++;
        this.CurrentFrame = BusyGlyphSequence.GetFrame(this.currentFrameIndex);
    }
}
