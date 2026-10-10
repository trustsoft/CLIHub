namespace CLIHub.Converters;

using System.Globalization;
using System.Windows.Data;

/// <summary>
///   Renders a text value in uppercase, for the Settings sidebar group labels.
/// </summary>
public sealed class UpperCaseTextConverter : IValueConverter
{
    /// <summary>
    ///   Returns the value as uppercase invariant text.
    /// </summary>
    /// <param name="value"> The source text. </param>
    /// <param name="targetType"> The target type of the binding. </param>
    /// <param name="parameter"> Unused. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns> The uppercase text, or an empty string when the value is null. </returns>
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString()?.ToUpperInvariant() ?? string.Empty;

    /// <summary>
    ///   Not supported; the converter is used in one-way bindings only.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
