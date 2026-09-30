namespace CLIHub.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
/// Reports whether a container is the first item of its list, so that row dividers are not
/// drawn above the first row.
/// </summary>
public sealed class IndexToDividerVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is int index && index > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
