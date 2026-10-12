namespace CLIHub.Controls;

using System.Windows;
using System.Windows.Controls;

/// <summary>
///   Arranges visible children in one direction with a uniform gap between adjacent children.
/// </summary>
public sealed class SpacedPanel : Panel
{
    /// <summary>
    ///   Identifies the <see cref="Orientation"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(SpacedPanel),
        new FrameworkPropertyMetadata(
            Orientation.Vertical,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    /// <summary>
    ///   Identifies the <see cref="Spacing"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing),
        typeof(double),
        typeof(SpacedPanel),
        new FrameworkPropertyMetadata(
            0d,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange,
            propertyChangedCallback: null,
            coerceValueCallback: CoerceSpacing));

    /// <summary>
    ///   Gets or sets the direction in which visible children are arranged.
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)this.GetValue(OrientationProperty);
        set => this.SetValue(OrientationProperty, value);
    }

    /// <summary>
    ///   Gets or sets the gap between adjacent visible children in device-independent units.
    ///   Values below zero and non-finite values are coerced to zero.
    /// </summary>
    public double Spacing
    {
        get => (double)this.GetValue(SpacingProperty);
        set => this.SetValue(SpacingProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var isVertical = this.Orientation == Orientation.Vertical;
        var childConstraint = isVertical
            ? new Size(availableSize.Width, double.PositiveInfinity)
            : new Size(double.PositiveInfinity, availableSize.Height);
        var totalLength = 0d;
        var crossLength = 0d;
        var visibleChildCount = 0;

        foreach (UIElement child in this.InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Measure(childConstraint);
            var desiredSize = child.DesiredSize;

            if (isVertical)
            {
                totalLength += desiredSize.Height;
                crossLength = Math.Max(crossLength, desiredSize.Width);
            }
            else
            {
                totalLength += desiredSize.Width;
                crossLength = Math.Max(crossLength, desiredSize.Height);
            }

            visibleChildCount++;
        }

        if (visibleChildCount > 1)
        {
            totalLength += (visibleChildCount - 1) * this.Spacing;
        }

        return isVertical
            ? new Size(crossLength, totalLength)
            : new Size(totalLength, crossLength);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var isVertical = this.Orientation == Orientation.Vertical;
        var currentLength = 0d;
        var hasArrangedChild = false;

        foreach (UIElement child in this.InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (hasArrangedChild)
            {
                currentLength += this.Spacing;
            }

            var desiredSize = child.DesiredSize;
            var childRect = isVertical
                ? new Rect(0, currentLength, finalSize.Width, desiredSize.Height)
                : new Rect(currentLength, 0, desiredSize.Width, finalSize.Height);

            child.Arrange(childRect);
            currentLength += isVertical ? desiredSize.Height : desiredSize.Width;
            hasArrangedChild = true;
        }

        return finalSize;
    }

    private static object CoerceSpacing(DependencyObject d, object baseValue)
    {
        var spacing = (double)baseValue;
        return double.IsFinite(spacing) && spacing >= 0d ? spacing : 0d;
    }
}
