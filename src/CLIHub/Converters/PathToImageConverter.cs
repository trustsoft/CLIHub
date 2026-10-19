namespace CLIHub.Converters;

using System.Globalization;
using System.Windows.Data;

using CLIHub.Imaging;

/// <summary>
///   Converts a logo file path to a shared frozen <see cref="System.Windows.Media.ImageSource" />
///   through the application-wide <see cref="LogoImageSourceCache" />. Null, missing, and
///   undecodable paths yield the default logo image instead of null.
/// </summary>
public class PathToImageConverter : IValueConverter
{
    /// <summary>
    ///   The application resource key the converter is registered under at startup.
    /// </summary>
    public const string ResourceKey = "PathToImage";

    private readonly LogoImageSourceCache imageCache;

    /// <summary>
    ///   Creates the converter over the shared decoded-image cache.
    /// </summary>
    /// <param name="imageCache"> The cache producing one frozen image per distinct path. </param>
    public PathToImageConverter(LogoImageSourceCache imageCache)
    {
        this.imageCache = imageCache ?? throw new ArgumentNullException(nameof(imageCache));
    }

    /// <summary>
    ///   Returns the cached frozen image for the given path, or the default logo image.
    /// </summary>
    /// <param name="value"> The image file path. </param>
    /// <param name="targetType"> The target type of the binding. </param>
    /// <param name="parameter"> Unused. </param>
    /// <param name="culture"> Unused. </param>
    /// <returns> The shared frozen image for the path, or the default logo image. </returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        this.imageCache.GetOrLoad(value as string);

    /// <summary>
    ///   Not supported; the converter is used in one-way bindings only.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
