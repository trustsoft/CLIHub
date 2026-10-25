namespace CLIHub.Controls;

using System.Windows;
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
///   optional danger-toned alert line, with an optional leading icon block.
/// </summary>
/// <remarks>
///   The icon always renders in the primary text brush, so a summary row reads as one block of text.
///   <see cref="SummaryCardTone.Neutral" /> is the muted tone and renders <see cref="Value" /> in the
///   primary text tone. <see cref="Alert" /> always renders in the danger tone and has no tone
///   property of its own, because every alert is a problem statement.
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
    ///   Identifies the <see cref="Alert" /> property.
    /// </summary>
    public static readonly DependencyProperty AlertProperty = DependencyProperty.Register(
        nameof(Alert), typeof(string), typeof(SummaryCard), new PropertyMetadata(default(string)));

    /// <summary>
    ///   Identifies the <see cref="AlertToolTip" /> property.
    /// </summary>
    public static readonly DependencyProperty AlertToolTipProperty = DependencyProperty.Register(
        nameof(AlertToolTip), typeof(string), typeof(SummaryCard), new PropertyMetadata(default(string)));

    /// <summary>
    ///   Identifies the <see cref="IsAlertVisible" /> property.
    /// </summary>
    public static readonly DependencyProperty IsAlertVisibleProperty = DependencyProperty.Register(
        nameof(IsAlertVisible), typeof(bool), typeof(SummaryCard), new PropertyMetadata(false));

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
    ///   An optional danger-toned line under the detail; it is shown only while
    ///   <see cref="IsAlertVisible" /> is <see langword="true" /> and this text is non-empty.
    /// </summary>
    public string? Alert
    {
        get => (string?)this.GetValue(AlertProperty);
        set => this.SetValue(AlertProperty, value);
    }

    /// <summary>
    ///   An optional tooltip for the alert line; no tooltip is shown when empty.
    /// </summary>
    public string? AlertToolTip
    {
        get => (string?)this.GetValue(AlertToolTipProperty);
        set => this.SetValue(AlertToolTipProperty, value);
    }

    /// <summary>
    ///   Whether the alert line is shown. The alert must also have non-empty text.
    /// </summary>
    public bool IsAlertVisible
    {
        get => (bool)this.GetValue(IsAlertVisibleProperty);
        set => this.SetValue(IsAlertVisibleProperty, value);
    }
}
