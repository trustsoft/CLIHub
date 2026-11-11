namespace CLIHub.Converters;

using System.Globalization;
using System.Windows.Data;

/// <summary>
///   Base class for one-way multi-value converters: the derived class implements only the forward
///   <see cref="Convert" />, while <see cref="ConvertBack" /> is owned here and always refuses
///   back-conversion, because the converter is used in one-way bindings only.
/// </summary>
public abstract class OneWayMultiValueConverter : IMultiValueConverter
{
    /// <summary>
    ///   Converts the source values for the one-way binding.
    /// </summary>
    /// <param name="values"> The source values produced by the binding. </param>
    /// <param name="targetType"> The target type of the binding. </param>
    /// <param name="parameter"> The converter parameter, or null when none is supplied. </param>
    /// <param name="culture"> The culture of the binding. </param>
    /// <returns> The converted value. </returns>
    public abstract object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture);

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
