namespace CLIHub.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
///   Shows a page UserControl only while the bound page key matches the page key supplied as the
///   converter parameter, which replaces a per-page visibility trigger set.
/// </summary>
public sealed class PageKeyVisibilityConverter : IValueConverter
{
    /// <summary>
    ///   Compares the selected page key with the key of the page that owns the binding.
    /// </summary>
    /// <param name="value"> The selected page key, normally <c> SelectedPage.Key </c>. </param>
    /// <param name="targetType"> The target type of the binding. </param>
    /// <param name="parameter"> The page key the element belongs to. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns>
    ///   <see cref="Visibility.Visible" /> when both keys are equal; otherwise
    ///   <see cref="Visibility.Collapsed" />, including when either side is missing.
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var pageKey = value as string;
        var expectedKey = parameter as string;

        return !string.IsNullOrEmpty(expectedKey) && string.Equals(pageKey, expectedKey, StringComparison.Ordinal)
            ? Visibility.Visible
            : Visibility.Collapsed;
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
