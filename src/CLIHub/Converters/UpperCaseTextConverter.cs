namespace CLIHub.Converters;

using System.Globalization;

/// <summary>
///   Renders a text value in uppercase, for the Settings sidebar group labels.
/// </summary>
public sealed class UpperCaseTextConverter : OneWayValueConverter
{
    /// <summary>
    ///   Returns the value as uppercase invariant text.
    /// </summary>
    /// <param name="value"> The source text. </param>
    /// <param name="targetType"> The target type of the binding. </param>
    /// <param name="parameter"> Unused. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns> The uppercase text, or an empty string when the value is null. </returns>
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString()?.ToUpperInvariant() ?? string.Empty;
}
