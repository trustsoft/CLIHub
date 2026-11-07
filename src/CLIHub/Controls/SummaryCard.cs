namespace CLIHub.Controls;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

/// <summary>
///   The tone of a <see cref="SummaryCard" /> icon or value line.
/// </summary>
public enum SummaryCardTone
{
    /// <summary>
    ///   The neutral tone: the value uses the primary text brush.
    /// </summary>
    Neutral,

    /// <summary> The accent tone. </summary>
    Accent,

    /// <summary>
    ///   The fully muted tone: the value uses the muted text brush. Used for a value that is a
    ///   placeholder rather than a measurement, such as an unset selection.
    /// </summary>
    Muted,

    /// <summary> The success (green) tone. </summary>
    Success,

    /// <summary> The danger (red) tone. </summary>
    Danger
}

/// <summary>
///   A compact metric card: a muted label, a prominent value, an optional muted detail line, and an
///   optional danger inline chip and icon badge, with an optional leading icon block.
/// </summary>
/// <remarks>
///   The icon always renders in the primary text brush, so a summary row reads as one block of text.
///   <see cref="SummaryCardTone.Neutral" /> is the muted tone and renders <see cref="Value" /> in the
///   primary text tone. <see cref="BadgeText" /> and <see cref="BadgeCount" /> always render in the
///   danger tone and have no tone property of their own, because every badge is a problem statement.
/// </remarks>
public sealed class SummaryCard : Control
{
    /// <summary>
    ///   Identifies the <see cref="Icon" /> property.
    /// </summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(string), typeof(SummaryCard), new PropertyMetadata(string.Empty));

    /// <summary>
    ///   Identifies the <see cref="Label" /> property.
    /// </summary>
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(SummaryCard), new PropertyMetadata(string.Empty));

    /// <summary>
    ///   Identifies the <see cref="Value" /> property.
    /// </summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(SummaryCard), new PropertyMetadata(string.Empty));

    /// <summary>
    ///   Identifies the <see cref="ValueTone" /> property.
    /// </summary>
    public static readonly DependencyProperty ValueToneProperty = DependencyProperty.Register(
        nameof(ValueTone), typeof(SummaryCardTone), typeof(SummaryCard), new PropertyMetadata(SummaryCardTone.Neutral));

    /// <summary>
    ///   Identifies the <see cref="ValueToolTip" /> property.
    /// </summary>
    public static readonly DependencyProperty ValueToolTipProperty = DependencyProperty.Register(
        nameof(ValueToolTip), typeof(string), typeof(SummaryCard), new PropertyMetadata(default(string)));

    /// <summary>
    ///   Identifies the <see cref="Detail" /> property.
    /// </summary>
    public static readonly DependencyProperty DetailProperty = DependencyProperty.Register(
        nameof(Detail), typeof(string), typeof(SummaryCard), new PropertyMetadata(default(string)));

    /// <summary>
    ///   Identifies the <see cref="DetailToolTip" /> property.
    /// </summary>
    public static readonly DependencyProperty DetailToolTipProperty = DependencyProperty.Register(
        nameof(DetailToolTip), typeof(string), typeof(SummaryCard), new PropertyMetadata(default(string)));

    /// <summary>
    ///   Identifies the <see cref="BadgeText" /> property.
    /// </summary>
    public static readonly DependencyProperty BadgeTextProperty = DependencyProperty.Register(
        nameof(BadgeText), typeof(string), typeof(SummaryCard), new PropertyMetadata(default(string)));

    /// <summary>
    ///   Identifies the <see cref="BadgeTooltip" /> property.
    /// </summary>
    public static readonly DependencyProperty BadgeTooltipProperty = DependencyProperty.Register(
        nameof(BadgeTooltip), typeof(string), typeof(SummaryCard), new PropertyMetadata(default(string)));

    /// <summary>
    ///   Identifies the <see cref="IsBadgeVisible" /> property.
    /// </summary>
    public static readonly DependencyProperty IsBadgeVisibleProperty = DependencyProperty.Register(
        nameof(IsBadgeVisible), typeof(bool), typeof(SummaryCard), new PropertyMetadata(false));

    /// <summary>
    ///   Identifies the <see cref="BadgeCount" /> property.
    /// </summary>
    public static readonly DependencyProperty BadgeCountProperty = DependencyProperty.Register(
        nameof(BadgeCount), typeof(string), typeof(SummaryCard), new PropertyMetadata(default(string)));

    static SummaryCard()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(SummaryCard),
            new FrameworkPropertyMetadata(typeof(SummaryCard)));
    }

    /// <summary>
    ///   The Segoe MDL2 Assets glyph shown in the icon block.
    /// </summary>
    public string Icon
    {
        get => (string)this.GetValue(IconProperty);
        set => this.SetValue(IconProperty, value);
    }

    /// <summary>
    ///   The muted caption above the value.
    /// </summary>
    public string Label
    {
        get => (string)this.GetValue(LabelProperty);
        set => this.SetValue(LabelProperty, value);
    }

    /// <summary>
    ///   The primary value shown in the card.
    /// </summary>
    public string Value
    {
        get => (string)this.GetValue(ValueProperty);
        set => this.SetValue(ValueProperty, value);
    }

    /// <summary>
    ///   The tone of the value line. <see cref="SummaryCardTone.Neutral" /> renders it in the primary
    ///   text tone.
    /// </summary>
    public SummaryCardTone ValueTone
    {
        get => (SummaryCardTone)this.GetValue(ValueToneProperty);
        set => this.SetValue(ValueToneProperty, value);
    }

    /// <summary>
    ///   An optional tooltip for the value line; no tooltip is shown when empty.
    /// </summary>
    public string? ValueToolTip
    {
        get => (string?)this.GetValue(ValueToolTipProperty);
        set => this.SetValue(ValueToolTipProperty, value);
    }

    /// <summary>
    ///   An optional muted detail line under the value; the line is hidden while this is empty.
    /// </summary>
    public string? Detail
    {
        get => (string?)this.GetValue(DetailProperty);
        set => this.SetValue(DetailProperty, value);
    }

    /// <summary>
    ///   An optional tooltip for the detail line; no tooltip is shown when empty.
    /// </summary>
    public string? DetailToolTip
    {
        get => (string?)this.GetValue(DetailToolTipProperty);
        set => this.SetValue(DetailToolTipProperty, value);
    }

    /// <summary>
    ///   The compact danger chip label shown inline with the value, e.g. "1 missing"; hidden when
    ///   null or empty.
    /// </summary>
    public string? BadgeText
    {
        get => (string?)this.GetValue(BadgeTextProperty);
        set => this.SetValue(BadgeTextProperty, value);
    }

    /// <summary>
    ///   Tooltip for both the inline chip and the icon count badge; one source of truth for the
    ///   status description.
    /// </summary>
    public string? BadgeTooltip
    {
        get => (string?)this.GetValue(BadgeTooltipProperty);
        set => this.SetValue(BadgeTooltipProperty, value);
    }

    /// <summary>
    ///   Master visibility flag; when false both the chip and the icon count badge are hidden.
    /// </summary>
    public bool IsBadgeVisible
    {
        get => (bool)this.GetValue(IsBadgeVisibleProperty);
        set => this.SetValue(IsBadgeVisibleProperty, value);
    }

    /// <summary>
    ///   Count label for the icon block overlay badge, e.g. "1"; hidden when null or empty.
    /// </summary>
    public string? BadgeCount
    {
        get => (string?)this.GetValue(BadgeCountProperty);
        set => this.SetValue(BadgeCountProperty, value);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new SummaryCardAutomationPeer(this);

    /// <summary>
    ///   Exposes a <see cref="SummaryCard" /> to UI Automation as a named group so screen readers
    ///   announce the card's label, value, detail, and badge status without requiring keyboard
    ///   focus on the card.
    /// </summary>
    private sealed class SummaryCardAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="SummaryCardAutomationPeer" /> class.
        /// </summary>
        /// <param name="owner"> The summary card this peer exposes. </param>
        public SummaryCardAutomationPeer(SummaryCard owner)
            : base(owner)
        {
        }

        /// <summary> Returns the card's UI Automation class name. </summary>
        /// <returns> The class name "SummaryCard". </returns>
        protected override string GetClassNameCore() => "SummaryCard";

        /// <summary> Returns the card's UI Automation control type. </summary>
        /// <returns>
        ///   <see cref="AutomationControlType.Group" />, because a summary card is a labelled
        ///   grouping of related content rather than a plain text run.
        /// </returns>
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        /// <summary> Places the card in the UI Automation control view. </summary>
        /// <returns>
        ///   <see langword="true" />, so structural navigation and screen readers reach the card.
        /// </returns>
        protected override bool IsControlElementCore() => true;

        /// <summary> Places the card in the UI Automation content view. </summary>
        /// <returns>
        ///   <see langword="true" />, so content-only navigation includes the card.
        /// </returns>
        protected override bool IsContentElementCore() => true;

        /// <summary>
        ///   Returns the card's accessible name. An explicit
        ///   <see cref="AutomationProperties.NameProperty" /> (the attached "Name" property) set on
        ///   the card wins; otherwise the label and value lead as one part, followed by the detail
        ///   and, when the badge is visible, the badge status description (the badge tooltip,
        ///   falling back to the badge text).
        /// </summary>
        /// <returns>
        ///   The composed name with parts joined by ", ", or the empty string when every part is
        ///   empty.
        /// </returns>
        protected override string GetNameCore()
        {
            var card = (SummaryCard)this.Owner;

            var explicitName = AutomationProperties.GetName(card);
            if (!string.IsNullOrEmpty(explicitName))
            {
                return explicitName;
            }

            var parts = new List<string>();

            var leading = string.Join(
                " ",
                new[] { card.Label, card.Value }.Where(part => !string.IsNullOrEmpty(part)));
            if (!string.IsNullOrEmpty(leading))
            {
                parts.Add(leading);
            }

            if (!string.IsNullOrEmpty(card.Detail))
            {
                parts.Add(card.Detail);
            }

            if (card.IsBadgeVisible)
            {
                var status = !string.IsNullOrEmpty(card.BadgeTooltip) ? card.BadgeTooltip : card.BadgeText;
                if (!string.IsNullOrEmpty(status))
                {
                    parts.Add(status);
                }
            }

            return string.Join(", ", parts);
        }
    }
}
