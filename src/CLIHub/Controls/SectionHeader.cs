namespace CLIHub.Controls;

using System.Windows;
using System.Windows.Controls;

/// <summary>
///   A muted uppercase section label with an optional accent marker.
/// </summary>
public sealed class SectionHeader : ContentControl
{
    /// <summary>
    ///   Identifies the <see cref="Text"/> property.
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty));

    /// <summary>
    ///   Identifies the <see cref="ShowMarker"/> property.
    /// </summary>
    public static readonly DependencyProperty ShowMarkerProperty = DependencyProperty.Register(
        nameof(ShowMarker), typeof(bool), typeof(SectionHeader), new PropertyMetadata(true));

    /// <summary>
    ///   The section label text (already uppercased by the caller).
    /// </summary>
    public string Text
    {
        get => (string)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>
    ///   Whether to show the accent marker before the label.
    /// </summary>
    public bool ShowMarker
    {
        get => (bool)this.GetValue(ShowMarkerProperty);
        set => this.SetValue(ShowMarkerProperty, value);
    }
}
