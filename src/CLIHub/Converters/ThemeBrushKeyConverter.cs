namespace CLIHub.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Media;

/// <summary>
///   Resolves a theme preview brush resource key, such as <c> ThemePreviewDarkBrush </c>, to the
///   brush declared in the application resources.
/// </summary>
public sealed class ThemeBrushKeyConverter : OneWayValueConverter
{
    /// <summary>
    ///   Looks the key up in the application resources, which include the merged palette and theme
    ///   style dictionaries.
    /// </summary>
    /// <param name="value"> The brush resource key. </param>
    /// <param name="targetType"> The target type of the binding. </param>
    /// <param name="parameter"> Unused. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns> The resolved brush, or <see cref="DependencyProperty.UnsetValue" /> when unavailable. </returns>
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key
            || string.IsNullOrWhiteSpace(key)
            || Application.Current is not { } application)
        {
            return DependencyProperty.UnsetValue;
        }

        return application.TryFindResource(key) as Brush ?? DependencyProperty.UnsetValue;
    }
}
