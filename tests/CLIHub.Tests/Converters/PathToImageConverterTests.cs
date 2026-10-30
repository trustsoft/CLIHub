namespace CLIHub.Tests.Converters;

using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using CLIHub.Converters;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Imaging;
using static CLIHub.Tests.Support.TestHelpers;

public sealed class PathToImageConverterTests : IDisposable
{
    private readonly string tempDir;

    public PathToImageConverterTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), "clihub-pathconverter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Convert_ExistingPath_DelegatesToCacheAndReturnsFrozenImage()
    {
        RunOnSta(() =>
        {
            var path = this.WritePng("logo.png");
            var cache = new LogoImageSourceCache(new LogoOptions(null));
            var converter = new PathToImageConverter(cache);

            var converted = converter.Convert(path, typeof(ImageSource), null, CultureInfo.InvariantCulture);

            Assert.Same(cache.GetOrLoad(path), converted);
            Assert.True(converted is BitmapImage image && image.IsFrozen);
        }, "STA converter test failed.");
    }

    [Fact]
    public void Convert_NullOrMissingPath_ReturnsDefaultImage()
    {
        RunOnSta(() =>
        {
            var defaultPath = this.WritePng("default.png");
            var cache = new LogoImageSourceCache(new LogoOptions(defaultPath));
            var converter = new PathToImageConverter(cache);

            var fromNull = converter.Convert(null, typeof(ImageSource), null, CultureInfo.InvariantCulture);
            var fromMissing = converter.Convert(Path.Combine(this.tempDir, "gone.png"), typeof(ImageSource), null, CultureInfo.InvariantCulture);

            Assert.Same(cache.GetOrLoad(null), fromNull);
            Assert.Same(fromNull, fromMissing);
        }, "STA converter test failed.");
    }

    [Fact]
    public void ConvertBack_AnyValue_Throws()
    {
        var converter = new PathToImageConverter(new LogoImageSourceCache(new LogoOptions(null)));

        Assert.Throws<NotSupportedException>(
            () => converter.ConvertBack(null, typeof(string), null, CultureInfo.InvariantCulture));
    }

    private string WritePng(string fileName)
    {
        var path = Path.Combine(this.tempDir, fileName);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Brushes.SteelBlue, null, new Rect(0, 0, 1, 1));
        }

        var bitmap = new RenderTargetBitmap(1, 1, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        File.WriteAllBytes(path, stream.ToArray());
        return path;
    }
}
