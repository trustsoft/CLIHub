namespace CLIHub.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
///   Collapses an element whose bound text is absent, so an optional header or summary slot takes
///   no space instead of reserving an empty strip.
/// </summary>
public sealed class EmptyToCollapsedVisibilityConverter : IValueConverter
{
    /// <summary>
    ///   Maps missing text to <see cref="Visibility.Collapsed" /> and any other value to
    ///   <see cref="Visibility.Visible" />.
    /// </summary>
    /// <param name="value"> The text, typically a template resource key from page metadata. </param>
    /// <param name="targetType"> The target type of the binding. </param>
    /// <param name="parameter"> Unused. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns> The slot visibility for the bound value. </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null || ReferenceEquals(value, DependencyProperty.UnsetValue))
        {
            return Visibility.Collapsed;
        }

        return value is string text && string.IsNullOrWhiteSpace(text)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    /// <summary>
    ///   Not supported; the converter is used in one-way bindings only.
    /// </summary>
    /// <param name="value"> Unused. </param>
    /// <param name="targetType"> Unused. </param>
    /// <param name="parameter"> Unused. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns> Never returns. </returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
