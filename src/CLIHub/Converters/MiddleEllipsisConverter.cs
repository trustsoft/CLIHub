namespace CLIHub.Converters;

using CLIHub.Core.Formatting;
using System.Globalization;
using System.Windows.Data;

/// <summary>
/// Shortens a text (typically a project path) in the middle so that it fits the width
/// available in a row, keeping both its beginning and its end visible.
/// </summary>
public sealed class MiddleEllipsisConverter : IMultiValueConverter
{
    /// <summary>
    /// Approximate advance width of one character at the row font size, used to turn the
    /// available pixel width into a character budget. Deliberately conservative.
    /// </summary>
    private const double AverageCharacterWidth = 6.5;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] is not string text || string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (values.Length < 2 || values[1] is not double availableWidth || double.IsNaN(availableWidth))
        {
            return text;
        }

        var budget = (int)Math.Floor(availableWidth / AverageCharacterWidth);

        return budget <= 0 ? text : MiddleEllipsisFormatter.Format(text, budget);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
