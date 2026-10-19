namespace CLIHub.Tests.Imaging;

using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Imaging;

public sealed class LogoImageSourceCacheTests : IDisposable
{
    private readonly string tempDir;

    public LogoImageSourceCacheTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), "clihub-logosource-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.tempDir, recursive: true); } catch { }
    }

    private string WritePng(string fileName)
    {
        var path = Path.Combine(this.tempDir, fileName);
        File.WriteAllBytes(path, CreatePngBytes());
        return path;
    }

    private LogoImageSourceCache CreateCache(string? defaultLogoPath = null) =>
        new(new LogoOptions(defaultLogoPath));

    [Fact]
    public void GetOrLoad_RepeatedPath_ReturnsSameFrozenInstance()
    {
        RunOnSta(() =>
        {
            var path = this.WritePng("logo.png");
            var cache = this.CreateCache();

            var first = cache.GetOrLoad(path);
            var second = cache.GetOrLoad(path);

            Assert.Same(first, second);
            Assert.True(first is BitmapImage image && image.IsFrozen);
        });
    }

    [Fact]
    public void GetOrLoad_NullOrEmptyPath_ReturnsDefaultInstance()
    {
        RunOnSta(() =>
        {
            var defaultPath = this.WritePng("default.png");
            var cache = this.CreateCache(defaultLogoPath: defaultPath);

            var fromNull = cache.GetOrLoad(null);
            var fromEmpty = cache.GetOrLoad(string.Empty);
            var fromWhitespace = cache.GetOrLoad("   ");

            Assert.Same(fromNull, fromEmpty);
            Assert.Same(fromNull, fromWhitespace);
            Assert.True(fromNull is BitmapImage image && image.IsFrozen);
        });
    }

    [Fact]
    public void GetOrLoad_MissingFile_ReturnsDefaultInstance()
    {
        RunOnSta(() =>
        {
            var defaultPath = this.WritePng("default.png");
            var cache = this.CreateCache(defaultLogoPath: defaultPath);

            var missing = cache.GetOrLoad(Path.Combine(this.tempDir, "gone.png"));

            var deletedPath = this.WritePng("deleted.png");
            File.Delete(deletedPath);
            var afterDelete = cache.GetOrLoad(deletedPath);

            Assert.Same(cache.GetOrLoad(null), missing);
            Assert.Same(missing, afterDelete);
        });
    }

    [Fact]
    public void GetOrLoad_UndecodableFile_ReturnsDefaultInstance()
    {
        RunOnSta(() =>
        {
            var defaultPath = this.WritePng("default.png");
            var cache = this.CreateCache(defaultLogoPath: defaultPath);

            var notAnImage = Path.Combine(this.tempDir, "not-image.png");
            File.WriteAllText(notAnImage, "this is not an image");

            var result = cache.GetOrLoad(notAnImage);

            Assert.Same(cache.GetOrLoad(null), result);
        });
    }

    [Fact]
    public void GetOrLoad_DefaultLogoPath_IsDecodedOnceAndShared()
    {
        RunOnSta(() =>
        {
            var defaultPath = this.WritePng("default.png");
            var logoPath = this.WritePng("logo.png");
            var cache = this.CreateCache(defaultLogoPath: defaultPath);

            var viaNull = cache.GetOrLoad(null);
            var viaMissing = cache.GetOrLoad(Path.Combine(this.tempDir, "missing.png"));
            var viaUndecodable = this.WritePng("broken.png");
            File.WriteAllText(viaUndecodable, "corrupt");

            Assert.Same(viaNull, viaMissing);
            Assert.Same(viaNull, cache.GetOrLoad(viaUndecodable));
            Assert.NotSame(viaNull, cache.GetOrLoad(logoPath));
        });
    }

    /// <summary>
    ///   Builds a minimal but fully valid 1x1 PNG so decode tests exercise a real decoder.
    /// </summary>
    private static byte[] CreatePngBytes()
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(Brushes.Firebrick, null, new Rect(0, 0, 1, 1));
        }

        var bitmap = new RenderTargetBitmap(1, 1, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static void RunOnSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            throw new Xunit.Sdk.XunitException("STA image test failed.", exception);
        }
    }
}
