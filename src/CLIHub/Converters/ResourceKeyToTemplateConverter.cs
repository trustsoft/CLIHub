namespace CLIHub.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
///   Resolves a template resource key carried by page metadata, such as
///   <c> UpdatesHeaderTitleExtra </c>, to the <see cref="DataTemplate" /> declared for it.
/// </summary>
/// <remarks>
///   The lookup starts at the element that owns the binding, which walks the window resources and
///   then the application resources. SettingsWindow declares its page header and summary templates
///   in its own resources, and those are not reachable through
///   <see cref="Application.TryFindResource" /> alone.
/// </remarks>
public sealed class ResourceKeyToTemplateConverter : IMultiValueConverter
{
    /// <summary>
    ///   Looks the key up in the resource scope of the bound element.
    /// </summary>
    /// <param name="values">
    ///   The template resource key and the element that owns the binding. When the element is
    ///   missing, the application resources are searched instead.
    /// </param>
    /// <param name="targetType"> The target type of the binding. </param>
    /// <param name="parameter"> Unused. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns>
    ///   The resolved template, or <see cref="DependencyProperty.UnsetValue" /> when the key is
    ///   missing, empty, or not declared as a <see cref="DataTemplate" />.
    /// </returns>
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] is not string key || string.IsNullOrWhiteSpace(key))
        {
            return DependencyProperty.UnsetValue;
        }

        if (values.Length > 1 && values[1] is FrameworkElement scope)
        {
            return scope.TryFindResource(key) as DataTemplate ?? DependencyProperty.UnsetValue;
        }

        return Application.Current?.TryFindResource(key) as DataTemplate ?? DependencyProperty.UnsetValue;
    }

    /// <summary>
    ///   Not supported; the converter is used in one-way bindings only.
    /// </summary>
    /// <param name="value"> Unused. </param>
    /// <param name="targetTypes"> Unused. </param>
    /// <param name="parameter"> Unused. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns> Never returns. </returns>
    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
