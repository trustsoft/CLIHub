namespace CLIHub.Tests.Controls;

using System.Threading;
using System.Windows;
using System.Windows.Controls;

using CLIHub.Controls;

public sealed class SpacedPanelTests
{
    [Fact]
    public void MeasureAndArrange_VerticalWithSpacing_UsesCrossAxisConstraintAndGaps()
    {
        RunOnSta(() =>
        {
            var first = new ProbeElement(new Size(40, 10));
            var second = new ProbeElement(new Size(30, 20));
            var panel = CreatePanel(first, second, spacing: 5);

            panel.Measure(new Size(100, 200));
            panel.Arrange(new Rect(0, 0, 100, 200));

            Assert.Equal(new Size(40, 35), panel.DesiredSize);
            Assert.Equal(new Size(100, double.PositiveInfinity), first.LastMeasureConstraint);
            Assert.Equal(new Rect(0, 0, 100, 10), GetLayoutRect(first, panel));
            Assert.Equal(new Rect(0, 15, 100, 20), GetLayoutRect(second, panel));
        });
    }

    [Fact]
    public void MeasureAndArrange_HorizontalWithSpacing_UsesCrossAxisConstraintAndGaps()
    {
        RunOnSta(() =>
        {
            var first = new ProbeElement(new Size(10, 20));
            var second = new ProbeElement(new Size(30, 15));
            var panel = CreatePanel(first, second, Orientation.Horizontal, spacing: 4);

            panel.Measure(new Size(200, 50));
            panel.Arrange(new Rect(0, 0, 200, 50));

            Assert.Equal(new Size(44, 20), panel.DesiredSize);
            Assert.Equal(new Size(double.PositiveInfinity, 50), first.LastMeasureConstraint);
            Assert.Equal(new Rect(0, 0, 10, 50), GetLayoutRect(first, panel));
            Assert.Equal(new Rect(14, 0, 30, 50), GetLayoutRect(second, panel));
        });
    }

    [Fact]
    public void MeasureAndArrange_CollapsedChild_IsExcludedFromSizeAndSpacing()
    {
        RunOnSta(() =>
        {
            var first = new ProbeElement(new Size(10, 10));
            var collapsed = new ProbeElement(new Size(100, 100)) { Visibility = Visibility.Collapsed };
            var last = new ProbeElement(new Size(10, 10));
            var panel = CreatePanel(first, collapsed, last, spacing: 6);

            panel.Measure(new Size(100, 100));
            panel.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(10, 26), panel.DesiredSize);
            Assert.Equal(new Rect(0, 16, 100, 10), GetLayoutRect(last, panel));
        });
    }

    [Fact]
    public void MeasureAndArrange_AllChildrenCollapsed_ReportsNoDesiredSize()
    {
        RunOnSta(() =>
        {
            var first = new ProbeElement(new Size(10, 10)) { Visibility = Visibility.Collapsed };
            var second = new ProbeElement(new Size(20, 20)) { Visibility = Visibility.Collapsed };
            var panel = CreatePanel(first, second, spacing: 6);

            panel.Measure(new Size(100, 100));
            panel.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Size(), panel.DesiredSize);
        });
    }

    [Fact]
    public void Spacing_InvalidValue_IsCoercedToZero()
    {
        RunOnSta(() =>
        {
            var panel = new SpacedPanel { Spacing = -1 };

            Assert.Equal(0, panel.Spacing);

            panel.Spacing = double.NaN;
            Assert.Equal(0, panel.Spacing);

            panel.Spacing = double.PositiveInfinity;
            Assert.Equal(0, panel.Spacing);
        });
    }

    [Fact]
    public void OrientationAndSpacing_ChangesInvalidateLayout()
    {
        RunOnSta(() =>
        {
            var panel = new SpacedPanel();

            panel.Measure(new Size(100, 100));
            Assert.True(panel.IsMeasureValid);
            panel.Spacing = 8;
            Assert.False(panel.IsMeasureValid);
        });
    }

    [Fact]
    public void Arrange_WhenDesiredLengthExceedsFinalLength_DoesNotOverlapChildren()
    {
        RunOnSta(() =>
        {
            var first = new ProbeElement(new Size(20, 20));
            var second = new ProbeElement(new Size(20, 20));
            var panel = CreatePanel(first, second, spacing: 5);

            panel.Measure(new Size(20, 10));
            panel.Arrange(new Rect(0, 0, 20, 10));

            Assert.Equal(new Rect(0, 0, 20, 20), GetLayoutRect(first, panel));
            Assert.Equal(new Rect(0, 25, 20, 20), GetLayoutRect(second, panel));
        });
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
            throw new Xunit.Sdk.XunitException("STA layout test failed.", exception);
        }
    }

    private static Rect GetLayoutRect(FrameworkElement child, FrameworkElement parent)
    {
        var origin = child.TranslatePoint(new Point(), parent);
        return new Rect(origin, new Size(child.ActualWidth, child.ActualHeight));
    }

    private static SpacedPanel CreatePanel(
        params UIElement[] children)
    {
        return CreatePanel(children, Orientation.Vertical, 0);
    }

    private static SpacedPanel CreatePanel(
        UIElement first,
        UIElement second,
        Orientation orientation = Orientation.Vertical,
        double spacing = 0)
    {
        return CreatePanel(new[] { first, second }, orientation, spacing);
    }

    private static SpacedPanel CreatePanel(
        UIElement first,
        UIElement collapsed,
        UIElement last,
        double spacing)
    {
        return CreatePanel(new[] { first, collapsed, last }, Orientation.Vertical, spacing);
    }

    private static SpacedPanel CreatePanel(
        UIElement[] children,
        Orientation orientation,
        double spacing)
    {
        var panel = new SpacedPanel
        {
            Orientation = orientation,
            Spacing = spacing
        };

        foreach (var child in children)
        {
            panel.Children.Add(child);
        }

        return panel;
    }

    private sealed class ProbeElement : FrameworkElement
    {
        private readonly Size desiredSize;

        public ProbeElement(Size desiredSize)
        {
            this.desiredSize = desiredSize;
        }

        public Size LastMeasureConstraint { get; private set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            this.LastMeasureConstraint = availableSize;
            return this.desiredSize;
        }
    }
}
