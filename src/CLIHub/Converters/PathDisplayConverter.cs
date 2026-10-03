namespace CLIHub.Converters;

using CLIHub.Core.Formatting;
using CLIHub.Core.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

/// <summary>
///   Shortens a project path so that it fills the width offered by the row's text column, using
///   the configured path display style. The longest fitting form is found by measuring candidate
///   texts with the row's own typeface, so the row is filled without clipping.
/// </summary>
public sealed class PathDisplayConverter : IMultiValueConverter
{
    /// <summary>
    ///   Fallback character width, used only when no text element is available for measuring.
    /// </summary>
    private const double FallbackCharacterWidth = 6.5d;

    /// <summary>
    ///   Shortens the path text to the available column width using the configured display style.
    /// </summary>
    /// <param name="values">
    ///   Path text, available column width, display style, and the measuring <see cref="TextBlock"/>.
    /// </param>
    /// <param name="targetType"> The target type of the binding. </param>
    /// <param name="parameter"> Unused. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns> The text, shortened when needed; empty for missing text. </returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] is not string text || string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (values.Length < 2 || values[1] is not double available || double.IsNaN(available))
        {
            // The text column reports its width from the first arrange on; before that there is
            // no honest budget. An empty stub keeps the content-sized window at its minimum
            // width, while the full path here would blow it up to the longest path's width.
            return string.Empty;
        }

        var style = values.Length > 2 && values[2] is PathDisplayStyle displayStyle
            ? displayStyle
            : PathDisplayStyles.Default;

        if (available <= 0)
        {
            return string.Empty;
        }

        var textBlock = values.Length > 3 ? values[3] as TextBlock : null;
        var budget = textBlock is null
            ? (int)Math.Floor(available / FallbackCharacterWidth)
            : FindLargestFittingBudget(text, available, style, textBlock);

        return budget <= 0
            ? text
            : Format(text, budget, style);
    }

    /// <summary>
    ///   Not supported; the converter is used in one-way bindings only.
    /// </summary>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static string Format(string text, int budget, PathDisplayStyle style) =>
        style == PathDisplayStyle.MiddleEllipsis
            ? MiddleEllipsisFormatter.Format(text, budget)
            : PathLeftTrimFormatter.Format(text, budget);

    /// <summary>
    ///   Returns the largest character budget whose formatted text still fits the available width.
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
}
