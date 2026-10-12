namespace CLIHub.Behaviors;

using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

/// <summary>
///   Attached behaviors for combo boxes.
/// </summary>
public static class ComboBoxBehaviors
{
    /// <summary>
    ///   Identifies the <c>AutoWidthToItems</c> attached property. When true, the combo box sizes
    ///   itself to the widest item so the closed control and its drop-down share the same width.
    /// </summary>
    public static readonly DependencyProperty AutoWidthToItemsProperty = DependencyProperty.RegisterAttached(
        "AutoWidthToItems",
        typeof(bool),
        typeof(ComboBoxBehaviors),
        new PropertyMetadata(false, OnAutoWidthToItemsChanged));

    private static readonly DependencyProperty ObserverProperty = DependencyProperty.RegisterAttached(
        "Observer",
        typeof(AutoWidthObserver),
        typeof(ComboBoxBehaviors),
        new PropertyMetadata(null));

    /// <summary>
    ///   Sets whether the combo box sizes itself to its widest item.
    /// </summary>
    /// <param name="element"> The combo box. </param>
    /// <param name="value"> Whether to enable auto width. </param>
    public static void SetAutoWidthToItems(DependencyObject element, bool value) =>
        element.SetValue(AutoWidthToItemsProperty, value);

    /// <summary>
    ///   Gets whether the combo box sizes itself to its widest item.
    /// </summary>
    /// <param name="element"> The combo box. </param>
    /// <returns> Whether auto width is enabled. </returns>
    public static bool GetAutoWidthToItems(DependencyObject element) =>
        (bool)element.GetValue(AutoWidthToItemsProperty);

    private static void OnAutoWidthToItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox combo)
        {
            return;
        }

        if (e.NewValue is true)
        {
            combo.SetValue(ObserverProperty, new AutoWidthObserver(combo));
        }
        else if (combo.GetValue(ObserverProperty) is AutoWidthObserver observer)
        {
            observer.Detach();
            combo.ClearValue(ObserverProperty);
        }
    }

    private sealed class AutoWidthObserver
    {
        private const double ChromeWidth = 38;

        private readonly ComboBox combo;

        public AutoWidthObserver(ComboBox combo)
        {
            this.combo = combo;
            combo.Loaded += this.OnLoaded;
            ((INotifyCollectionChanged)combo.Items).CollectionChanged += this.OnItemsChanged;
        }

        public void Detach()
        {
            this.combo.Loaded -= this.OnLoaded;
            ((INotifyCollectionChanged)this.combo.Items).CollectionChanged -= this.OnItemsChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => this.UpdateWidth();

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => this.UpdateWidth();

        private void UpdateWidth()
        {
            var widest = 0.0;

            foreach (var item in this.combo.Items)
            {
                var presenter = new ContentPresenter
                {
                    Content = item,
                    ContentTemplate = this.combo.ItemTemplate,
                    ContentStringFormat = this.combo.ItemStringFormat
                };

                TextElement.SetFontFamily(presenter, this.combo.FontFamily);
                TextElement.SetFontSize(presenter, this.combo.FontSize);
                presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                widest = Math.Max(widest, presenter.DesiredSize.Width);
            }

            if (widest > 0)
            {
                this.combo.Width = Math.Ceiling(widest) + ChromeWidth;
            }
        }
    }
}
