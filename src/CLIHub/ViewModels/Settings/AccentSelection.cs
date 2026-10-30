namespace CLIHub.ViewModels;

using CLIHub.Core.Models;
using CLIHub.Themes;

/// <summary>
///   Accent color selection section: presets, the custom entry, and custom-value memory.
/// </summary>
public sealed class AccentSelection : ObservableObject
{
    private static readonly AccentChoice CustomAccentChoice =
        new("Custom...", "Transparent", "RGB / Hex", true);

    private static readonly AccentChoice[] AccentChoices = AccentPalette.Presets
        .Select(preset => new AccentChoice(preset.Name, preset.Hex, preset.Hex, false))
        .Append(CustomAccentChoice)
        .ToArray();

    private readonly AccentSelectionMemory accentMemory;

    private string accentText = AccentPalette.DefaultHex;
    private bool customAccentSelected;

    /// <summary>
    ///   Creates the accent selection section.
    /// </summary>
    /// <param name="accentMemory"> Process-wide memory of the last custom accent. </param>
    public AccentSelection(AccentSelectionMemory accentMemory)
    {
        this.accentMemory = accentMemory ?? throw new ArgumentNullException(nameof(accentMemory));
    }

    /// <summary>
    ///   The accent color choices offered by the selector: the presets and the custom entry.
    /// </summary>
    public IReadOnlyList<AccentChoice> AccentOptions => AccentChoices;

    /// <summary>
    ///   The accent color text as <c> #RRGGBB </c>.
    /// </summary>
    public string AccentText
    {
        get => this.accentText;
        set
        {
            if (this.SetProperty(ref this.accentText, value))
            {
                var preset = this.FindAccentPreset(value);
                this.customAccentSelected = preset is null;
                if (preset is null && AccentPalette.TryNormalize(value, out var normalized))
                {
                    this.accentMemory.LastCustomAccent = normalized;
                }

                this.OnPropertyChanged(nameof(this.SelectedAccent));
                this.OnPropertyChanged(nameof(this.ActiveAccentLabel));
            }
        }
    }

    /// <summary>
    ///   The selected accent choice: the matching preset, or the custom entry for any other value.
    /// </summary>
    public AccentChoice SelectedAccent
    {
        get => this.customAccentSelected
            ? CustomAccentChoice
            : this.FindAccentPreset(this.accentText) ?? CustomAccentChoice;
        set
        {
            if (value is null)
            {
                return;
            }

            if (value.IsCustom)
            {
                if (this.accentMemory.LastCustomAccent is { } customAccent)
                {
                    this.AccentText = customAccent;
                    return;
                }

                if (this.customAccentSelected)
                {
                    return;
                }

                this.customAccentSelected = true;
                this.OnPropertyChanged(nameof(this.SelectedAccent));
                return;
            }

            this.AccentText = value.Hex;
        }
    }

    /// <summary>
    ///   The accent shown next to the selector title, e.g. "Active: Blue".
    /// </summary>
    public string ActiveAccentLabel
    {
        get
        {
            var preset = this.FindAccentPreset(this.accentText);
            return preset is null ? "Active: Custom" : $"Active: {preset.Name}";
        }
    }

    /// <summary>
    ///   Loads the accent from a stored color value.
    /// </summary>
    /// <param name="accentColor"> The stored accent color. </param>
    public void Load(string accentColor)
    {
        this.accentText = AccentPalette.Normalize(accentColor);
        this.customAccentSelected = this.FindAccentPreset(this.accentText) is null;
        if (this.customAccentSelected && this.accentMemory.LastCustomAccent is null)
        {
            this.accentMemory.LastCustomAccent = this.accentText;
        }

        this.OnPropertyChanged(nameof(this.AccentText));
        this.OnPropertyChanged(nameof(this.SelectedAccent));
        this.OnPropertyChanged(nameof(this.ActiveAccentLabel));
    }

    /// <summary>
    ///   Finds the preset matching a color value, ignoring the custom entry. Invalid or partial
    ///   values match nothing, so the selector shows the custom entry while the user types.
    /// </summary>
    private AccentChoice? FindAccentPreset(string value)
    {
        if (!AccentPalette.TryNormalize(value, out var normalized))
        {
            return null;
        }

        return AccentChoices.FirstOrDefault(choice =>
            !choice.IsCustom &&
            string.Equals(AccentPalette.Normalize(choice.Hex), normalized, StringComparison.Ordinal));
    }
}
