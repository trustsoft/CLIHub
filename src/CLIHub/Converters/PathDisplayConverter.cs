namespace CLIHub.Converters;

using CLIHub.Core.Formatting;
using CLIHub.Core.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

/// <summary>
/// Shortens a project path so that it fits the width available in a row, using the configured
/// path display style. The longest fitting form is found by measuring candidate texts with the
/// row's own typeface, so the row is filled without clipping.
/// </summary>
public sealed class PathDisplayConverter : IMultiValueConverter
{
    /// <summary>
    /// Width reserved for the favourite marker when the project is a favourite, so the path never
    /// runs into it: the 13px glyph plus its 8px margin in the project row template.
    /// </summary>
    private const double FavouriteMarkerWidth = 21d;

    /// <summary>
    /// Fallback character width, used only when no text element is available for measuring.
    /// </summary>
    private const double FallbackCharacterWidth = 6.5d;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] is not string text || string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (values.Length < 2 || values[1] is not double listWidth || double.IsNaN(listWidth))
        {
            return text;
        }

        var style = values.Length > 2 && values[2] is PathDisplayStyle displayStyle
            ? displayStyle
            : PathDisplayStyles.Default;

        // The list width is measured on the list, so the space taken by the row padding, the
        // thumbnail, the text margin and (when shown) the favourite marker is subtracted here.
        var insets = ParseInsets(parameter);
        if (values.Length > 3 && values[3] is true)
        {
            insets += FavouriteMarkerWidth;
        }

        var available = listWidth - insets;
        if (available <= 0)
        {
            return text;
        }

        var textBlock = values.Length > 4 ? values[4] as TextBlock : null;
        var budget = textBlock is null
            ? (int)Math.Floor(available / FallbackCharacterWidth)
            : FindLargestFittingBudget(text, available, style, textBlock);

        return budget <= 0
            ? text
            : Format(text, budget, style);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static string Format(string text, int budget, PathDisplayStyle style) =>
        style == PathDisplayStyle.MiddleEllipsis
            ? MiddleEllipsisFormatter.Format(text, budget)
            : PathLeftTrimFormatter.Format(text, budget);

    /// <summary>
    /// Returns the largest character budget whose formatted text still fits the available width.
    /// </summary>
    private static int FindLargestFittingBudget(string text, double available, PathDisplayStyle style, TextBlock textBlock)
    {
        if (Measure(text, textBlock) <= available)
        {
            return text.Length;
        }

        var low = 1;
        var high = text.Length;
        var best = 1;

        while (low <= high)
        {
            var middle = (low + high) / 2;

            if (Measure(Format(text, middle, style), textBlock) <= available)
            {
                best = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return best;
    }

    private static double Measure(string text, TextBlock textBlock)
    {
        try
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);

            return formatted.Width;
        }
        catch (InvalidOperationException)
        {
            // The element may not be attached to a visual tree yet; fall back to an estimate.
            return text.Length * FallbackCharacterWidth;
        }
    }

    private static double ParseInsets(object parameter)
    {
        if (parameter is double doubleValue)
        {
            return doubleValue;
        }

        return parameter is string text
            && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0d;
    }
}
