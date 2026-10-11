namespace CLIHub.Views;

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

        private readonly ComboBox _combo;

        public AutoWidthObserver(ComboBox combo)
        {
            _combo = combo;
            combo.Loaded += OnLoaded;
            ((INotifyCollectionChanged)combo.Items).CollectionChanged += OnItemsChanged;
        }

        public void Detach()
        {
            _combo.Loaded -= OnLoaded;
            ((INotifyCollectionChanged)_combo.Items).CollectionChanged -= OnItemsChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => UpdateWidth();

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateWidth();

        private void UpdateWidth()
        {
            var widest = 0.0;

            foreach (var item in _combo.Items)
            {
                var presenter = new ContentPresenter
                {
                    Content = item,
                    ContentTemplate = _combo.ItemTemplate,
                    ContentStringFormat = _combo.ItemStringFormat
                };

                TextElement.SetFontFamily(presenter, _combo.FontFamily);
                TextElement.SetFontSize(presenter, _combo.FontSize);
                presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                widest = Math.Max(widest, presenter.DesiredSize.Width);
            }

            if (widest > 0)
            {
                _combo.Width = Math.Ceiling(widest) + ChromeWidth;
            }
        }
    }
}
