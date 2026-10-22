namespace CLIHub.Tests.Converters;

using System.Globalization;
using System.Windows;

using CLIHub.Converters;

public sealed class EmptyToCollapsedVisibilityConverterTests
{
    private static readonly EmptyToCollapsedVisibilityConverter Converter = new();

    [Fact]
    public void Convert_NullValue_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert(null));
    }

    [Fact]
    public void Convert_UnsetValue_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert(DependencyProperty.UnsetValue));
    }

    [Fact]
    public void Convert_EmptyString_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert(string.Empty));
    }

    [Fact]
    public void Convert_WhitespaceString_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert("  "));
    }

    [Fact]
    public void Convert_ResourceKey_ReturnsVisible()
    {
        Assert.Equal(Visibility.Visible, Convert("UpdatesHeaderTitleExtra"));
    }

    [Fact]
    public void ConvertBack_AnyValue_Throws()
    {
        Assert.Throws<NotSupportedException>(
            () => Converter.ConvertBack(Visibility.Visible, typeof(string), null, CultureInfo.InvariantCulture));
    }

    private static object Convert(object? value) =>
        Converter.Convert(value, typeof(Visibility), null, CultureInfo.InvariantCulture);
}
