namespace CLIHub.Converters;

using System.Globalization;
using System.Windows;

/// <summary>
///   Reports whether a container is the first item of its list, so that row dividers are not
///   drawn above the first row.
/// </summary>
public sealed class IndexToDividerVisibilityConverter : OneWayValueConverter
{
    /// <summary>
    ///   Makes the divider visible for every item except the first.
    /// </summary>
    /// <param name="value"> The zero-based item index. </param>
    /// <param name="targetType"> The target type of the binding. </param>
    /// <param name="parameter"> Unused. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns> <see cref="Visibility.Visible" /> for indexes above zero; otherwise collapsed. </returns>
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int index && index > 0 ? Visibility.Visible : Visibility.Collapsed;
}
