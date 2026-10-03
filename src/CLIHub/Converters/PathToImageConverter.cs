namespace CLIHub.Converters;

using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

/// <summary>
///   Converts a file path to an <see cref="BitmapImage"/>, or null when the path is empty/missing.
/// </summary>
public class PathToImageConverter : IValueConverter
{
    /// <summary>
    ///   Loads the image at the given path, frozen so it can be shared across rows.
    /// </summary>
    /// <param name="value"> The image file path. </param>
    /// <param name="targetType"> The target type of the binding. </param>
    /// <param name="parameter"> Unused. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns> The loaded <see cref="BitmapImage"/>, or null when the path is empty or missing. </returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    /// <summary>
    ///   Not supported; the converter is used in one-way bindings only.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
