namespace CLIHub.ViewModels;

using CLIHub.Controls;
using CLIHub.Themes;

/// <summary>
///   Update status card shown in the Settings Updates page header.
/// </summary>
/// <param name="Title"> Status heading. </param>
/// <param name="Detail"> Version and last-check detail. </param>
/// <param name="Badge"> Badge text; empty hides the badge. </param>
/// <param name="Tone"> Badge visual tone. </param>
public sealed record SettingsUpdateCard(
    string Title,
    string Detail,
    string Badge,
    SettingBadgeTone Tone)
{
    /// <summary>
    ///   The glyph shown in the card's leading icon block, derived from the tone.
    /// </summary>
    public string Glyph => this.Tone switch
    {
        SettingBadgeTone.Success => IconGlyphs.Checkmark,
        SettingBadgeTone.Accent => IconGlyphs.Refresh,
        _ => IconGlyphs.Info
    };
}
