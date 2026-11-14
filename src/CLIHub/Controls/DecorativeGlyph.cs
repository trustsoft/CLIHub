namespace CLIHub.Controls;

using System.Windows.Automation.Peers;
using System.Windows.Controls;

/// <summary>
///   A TextBlock that is hidden from assistive technology.
///   Use this for decorative glyphs that duplicate adjacent text labels.
/// </summary>
public sealed class DecorativeGlyph : TextBlock
{
    /// <summary>
    ///   Creates an automation peer that returns null, excluding this element from the UIA tree.
    /// </summary>
    /// <returns>null.</returns>
    protected override AutomationPeer? OnCreateAutomationPeer() => null;
}
