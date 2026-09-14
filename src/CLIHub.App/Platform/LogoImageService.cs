using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CLIHub.App.Platform;

public sealed class LogoImageService
{
    private const int DecodeWidth = 64;

    private readonly Dictionary<string, ImageSource?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ImageSource _default;

    public LogoImageService()
    {
        _default = new BitmapImage(new Uri("pack://application:,,,/Assets/default-logo.png"));
    }

    public ImageSource Default => _default;

    public ImageSource GetImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return _default;
        }

        if (_cache.TryGetValue(path, out var cached))
        {
            return cached ?? _default;
        }

        var loaded = TryLoad(path);
        _cache[path] = loaded;
        return loaded ?? _default;
    }

    private static ImageSource? TryLoad(string path)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = DecodeWidth;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
