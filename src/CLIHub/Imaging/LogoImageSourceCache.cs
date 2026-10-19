namespace CLIHub.Imaging;

using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using CLIHub.Core.Infrastructure.Persistence;

/// <summary>
///   Decodes each distinct resolved logo path once into a frozen, shareable image instance:
///   every view bound to the same path reuses the same decoded pixels, so memory scales with
///   the number of distinct logos rather than with the number of image controls. Null, empty,
///   missing, and undecodable paths resolve to the default logo image instead of a blank.
/// </summary>
public sealed class LogoImageSourceCache
{
    private const string DefaultImageKey = "\0default-logo";

    private readonly ConcurrentDictionary<string, ImageSource> images = new(StringComparer.Ordinal);
    private readonly LogoOptions logoOptions;

    /// <summary>
    ///   Creates the cache over the shared logo options, which supply the default logo path.
    /// </summary>
    /// <param name="logoOptions"> The shared logo options. </param>
    public LogoImageSourceCache(LogoOptions logoOptions)
    {
        this.logoOptions = logoOptions ?? throw new ArgumentNullException(nameof(logoOptions));
    }

    /// <summary>
    ///   Returns the frozen image for the resolved path, decoding it at most once. A null or
    ///   empty path, a missing file, or an undecodable file yields the default logo image.
    /// </summary>
    /// <param name="path"> The resolved logo path, or null. </param>
    /// <returns> The shared frozen <see cref="ImageSource" /> to display. </returns>
    public ImageSource GetOrLoad(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return this.GetDefaultImage();
        }

        if (!File.Exists(path))
        {
            return this.GetDefaultImage();
        }

        try
        {
            return this.images.GetOrAdd(path, static p => Decode(p));
        }
        catch (Exception ex) when (IsDecodeFailure(ex))
        {
            return this.GetDefaultImage();
        }
    }

    private ImageSource GetDefaultImage()
    {
        var defaultPath = this.logoOptions.DefaultLogoPath;

        if (!string.IsNullOrWhiteSpace(defaultPath) && File.Exists(defaultPath))
        {
            try
            {
                return this.images.GetOrAdd(DefaultImageKey, _ => Decode(defaultPath));
            }
            catch (Exception ex) when (IsDecodeFailure(ex))
            {
            }
        }

        // Last resort when even the default asset is unusable: a frozen blank image so the
        // binding always receives a stable, thread-safe instance.
        return this.images.GetOrAdd(DefaultImageKey, _ => CreateBlankImage());
    }

    /// <summary>
    ///   True for the failure modes of decoding a present but unusable file: unreadable,
    ///   not an image format WPF knows, or corrupt image data.
    /// </summary>
    private static bool IsDecodeFailure(Exception ex) =>
        ex is IOException or NotSupportedException or FileFormatException or InvalidOperationException or UnauthorizedAccessException;

    private static BitmapImage Decode(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static DrawingImage CreateBlankImage()
    {
        var drawing = new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 1, 1)));
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }
}
