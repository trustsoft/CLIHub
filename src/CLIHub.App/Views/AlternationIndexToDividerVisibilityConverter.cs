using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CLIHub.App.Views;

/// <summary>
/// Converts a row alternation index to row-divider visibility: the divider
/// shows on every row except the first (index 0).
/// </summary>
public sealed class AlternationIndexToDividerVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int index && index > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
