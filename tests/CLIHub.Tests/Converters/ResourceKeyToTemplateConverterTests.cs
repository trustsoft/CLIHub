namespace CLIHub.Tests.Converters;

using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

using CLIHub.Converters;

public sealed class ResourceKeyToTemplateConverterTests
{
    private static readonly ResourceKeyToTemplateConverter Converter = new();

    [Fact]
    public void Convert_NullKey_ReturnsUnsetValue()
    {
        RunOnSta(() => Assert.Same(DependencyProperty.UnsetValue, Convert(new object?[] { null, new Grid() })));
    }

    [Fact]
    public void Convert_EmptyKey_ReturnsUnsetValue()
    {
        RunOnSta(() => Assert.Same(DependencyProperty.UnsetValue, Convert(new object?[] { string.Empty, new Grid() })));
    }

    [Fact]
    public void Convert_WhitespaceKey_ReturnsUnsetValue()
    {
        RunOnSta(() => Assert.Same(DependencyProperty.UnsetValue, Convert(new object?[] { "   ", new Grid() })));
    }

    [Fact]
    public void Convert_KeyDeclaredInScopeResources_ReturnsTemplate()
    {
        RunOnSta(() =>
        {
            var template = new DataTemplate();
            var scope = new Grid();
            scope.Resources["PageHeaderSlot"] = template;

            var converted = Convert(new object?[] { "PageHeaderSlot", scope });

            Assert.Same(template, converted);
        });
    }

    [Fact]
    public void Convert_KeyNotDeclared_ReturnsUnsetValue()
    {
        RunOnSta(() =>
        {
            var scope = new Grid();
            scope.Resources["PageHeaderSlot"] = new DataTemplate();

            var converted = Convert(new object?[] { "MissingHeaderSlot", scope });

            Assert.Same(DependencyProperty.UnsetValue, converted);
        });
    }

    [Fact]
    public void Convert_KeyResolvesToAnotherResourceKind_ReturnsUnsetValue()
    {
        RunOnSta(() =>
        {
            var scope = new Grid();
            scope.Resources["PageHeaderSlot"] = "not a template";

            var converted = Convert(new object?[] { "PageHeaderSlot", scope });

            Assert.Same(DependencyProperty.UnsetValue, converted);
        });
    }

    [Fact]
    public void Convert_NoScopeElement_ReturnsUnsetValueWithoutApplicationResources()
    {
        RunOnSta(() =>
        {
            // This test process creates no WPF Application, so the application-level fallback has
            // nothing to search and must report the key as unresolvable.
            Assert.Null(Application.Current);

            var converted = Convert(new object?[] { "UpdatesHeaderTitleExtra" });

            Assert.Same(DependencyProperty.UnsetValue, converted);
        });
    }

    [Fact]
    public void ConvertBack_AnyValue_Throws()
    {
        Assert.Throws<NotSupportedException>(
            () => Converter.ConvertBack(
                new DataTemplate(),
                new[] { typeof(string) },
                null,
                CultureInfo.InvariantCulture));
    }

    private static object Convert(object?[] values) =>
        Converter.Convert(values!, typeof(DataTemplate), null, CultureInfo.InvariantCulture);

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
            throw new Xunit.Sdk.XunitException("STA converter test failed.", exception);
        }
    }
}
