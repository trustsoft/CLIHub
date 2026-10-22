namespace CLIHub.Tests.Converters;

using System.Globalization;
using System.Windows;

using CLIHub.Converters;

public sealed class PageKeyVisibilityConverterTests
{
    private static readonly PageKeyVisibilityConverter Converter = new();

    [Fact]
    public void Convert_MatchingKey_ReturnsVisible()
    {
        Assert.Equal(Visibility.Visible, Convert("projects-registry", "projects-registry"));
    }

    [Fact]
    public void Convert_DifferentKey_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert("projects", "projects-registry"));
    }

    [Fact]
    public void Convert_NullPageKey_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert(null, "general"));
    }

    [Fact]
    public void Convert_UnsetPageKey_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert(DependencyProperty.UnsetValue, "general"));
    }

    [Fact]
    public void Convert_NullParameter_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert("general", null));
    }

    [Fact]
    public void Convert_NullOnBothSides_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert(null, null));
    }

    [Fact]
    public void ConvertBack_AnyValue_Throws()
    {
        Assert.Throws<NotSupportedException>(
            () => Converter.ConvertBack(Visibility.Visible, typeof(string), "general", CultureInfo.InvariantCulture));
    }

    private static object Convert(object? value, object? parameter) =>
        Converter.Convert(value, typeof(Visibility), parameter!, CultureInfo.InvariantCulture);
}
