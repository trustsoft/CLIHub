namespace CLIHub.Controls;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

/// <summary>
///   An editable numeric field with decrement and increment buttons and an optional unit suffix.
///   The value is exposed as <see cref="Text" /> so the field binds to the settings form's text
///   values; the buttons and the Up/Down keys clamp the edited number to <see cref="Minimum" />
///   and <see cref="Maximum" />.
/// </summary>
public sealed class NumericField : Control
{
    /// <summary>
    ///   Identifies the <see cref="Text" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(NumericField),
        new FrameworkPropertyMetadata(
            string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnTextChanged));

    /// <summary>
    ///   Identifies the <see cref="Minimum" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(int),
        typeof(NumericField),
        new PropertyMetadata(0, OnBoundsChanged));

    /// <summary>
    ///   Identifies the <see cref="Maximum" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(int),
        typeof(NumericField),
        new PropertyMetadata(int.MaxValue, OnBoundsChanged));

    /// <summary>
    ///   Identifies the <see cref="Step" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step),
        typeof(int),
        typeof(NumericField),
        new PropertyMetadata(
            1,
            propertyChangedCallback: null,
            coerceValueCallback: CoerceStep));

    /// <summary>
    ///   Identifies the <see cref="Unit" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit),
        typeof(string),
        typeof(NumericField),
        new PropertyMetadata(string.Empty));

    /// <summary>
    ///   Identifies the <see cref="Placeholder" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder),
        typeof(string),
        typeof(NumericField),
        new PropertyMetadata(string.Empty));

    private const string DecreaseButtonPartName = "PART_DecreaseButton";
    private const string IncreaseButtonPartName = "PART_IncreaseButton";
    private const string TextBoxPartName = "PART_TextBox";

    private ButtonBase? decreaseButton;
    private ButtonBase? increaseButton;
    private TextBox? textBox;

    static NumericField()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NumericField),
            new FrameworkPropertyMetadata(typeof(NumericField)));
    }

    /// <summary>
    ///   Gets or sets the numeric text shown in the field. The value is updated as the user types.
    /// </summary>
    public string Text
    {
        get => (string)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>
    ///   Gets or sets the smallest value the step buttons allow.
    /// </summary>
    public int Minimum
    {
        get => (int)this.GetValue(MinimumProperty);
        set => this.SetValue(MinimumProperty, value);
    }

    /// <summary>
    ///   Gets or sets the largest value the step buttons allow.
    /// </summary>
    public int Maximum
    {
        get => (int)this.GetValue(MaximumProperty);
        set => this.SetValue(MaximumProperty, value);
    }

    /// <summary>
    ///   Gets or sets the amount added or removed by one step. Values below one are coerced to one.
    /// </summary>
    public int Step
    {
        get => (int)this.GetValue(StepProperty);
        set => this.SetValue(StepProperty, value);
    }

    /// <summary>
    ///   Gets or sets a short unit suffix shown after the number, for example "min".
    ///   An empty value hides the suffix.
    /// </summary>
    public string Unit
    {
        get => (string)this.GetValue(UnitProperty);
        set => this.SetValue(UnitProperty, value);
    }

    /// <summary>
    ///   Gets or sets the muted hint shown while the field is empty and unfocused. When the hint
    ///   is a number, the step buttons use it as the starting value of an empty field.
    /// </summary>
    public string Placeholder
    {
        get => (string)this.GetValue(PlaceholderProperty);
        set => this.SetValue(PlaceholderProperty, value);
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (!e.Handled && this.textBox is not null)
        {
            this.textBox.Focus();
            this.textBox.CaretIndex = this.textBox.Text.Length;
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        base.OnPreviewMouseWheel(e);

        if (e.Handled || e.Delta == 0)
        {
            return;
        }

        var notches = Math.Max(1, Math.Abs(e.Delta / Mouse.MouseWheelDeltaForOneLine));
        this.StepBy(e.Delta > 0 ? this.Step * notches : -this.Step * notches);
        e.Handled = true;
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (this.decreaseButton is not null)
        {
            this.decreaseButton.Click -= this.OnDecreaseClick;
        }

        if (this.increaseButton is not null)
        {
            this.increaseButton.Click -= this.OnIncreaseClick;
        }

        if (this.textBox is not null)
        {
            this.textBox.PreviewKeyDown -= this.OnTextBoxPreviewKeyDown;
        }

        base.OnApplyTemplate();

        this.decreaseButton = this.GetTemplateChild(DecreaseButtonPartName) as ButtonBase;
        this.increaseButton = this.GetTemplateChild(IncreaseButtonPartName) as ButtonBase;
        this.textBox = this.GetTemplateChild(TextBoxPartName) as TextBox;

        if (this.decreaseButton is not null)
        {
            this.decreaseButton.Click += this.OnDecreaseClick;
        }

        if (this.increaseButton is not null)
        {
            this.increaseButton.Click += this.OnIncreaseClick;
        }

        if (this.textBox is not null)
        {
            this.textBox.PreviewKeyDown += this.OnTextBoxPreviewKeyDown;
        }

        this.UpdateButtons();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NumericField)d).UpdateButtons();

    private static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NumericField)d).UpdateButtons();

    private static object CoerceStep(DependencyObject d, object baseValue)
    {
        var step = (int)baseValue;
        return step > 0 ? step : 1;
    }

    private void OnDecreaseClick(object sender, RoutedEventArgs e) => this.StepBy(-this.Step);

    private void OnIncreaseClick(object sender, RoutedEventArgs e) => this.StepBy(this.Step);

    private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                this.StepBy(this.Step);
                e.Handled = true;
                break;
            case Key.Down:
                this.StepBy(-this.Step);
                e.Handled = true;
                break;
        }
    }

    private void StepBy(int delta)
    {
        var next = this.ResolveNextValue(delta);
        this.Text = next.ToString(CultureInfo.InvariantCulture);

        if (this.textBox is not null)
        {
            this.textBox.CaretIndex = this.textBox.Text.Length;
        }
    }

    private int ResolveNextValue(int delta)
    {
        if (TryParseNumber(this.Text, out var value))
        {
            return (int)Math.Clamp((long)value + delta, this.Minimum, this.Maximum);
        }

        // An empty field whose placeholder is a number shows the effective default; step from it.
        if (TryParseNumber(this.Placeholder, out var placeholder))
        {
            return (int)Math.Clamp((long)placeholder + delta, this.Minimum, this.Maximum);
        }

        return Math.Clamp(this.Minimum, this.Minimum, this.Maximum);
    }

    private int ResolveCurrentValue()
    {
        if (TryParseNumber(this.Text, out var value))
        {
            return Math.Clamp(value, this.Minimum, this.Maximum);
        }

        if (TryParseNumber(this.Placeholder, out var placeholder))
        {
            return Math.Clamp(placeholder, this.Minimum, this.Maximum);
        }

        return Math.Clamp(0, this.Minimum, this.Maximum);
    }

    private void UpdateButtons()
    {
        if (this.decreaseButton is null && this.increaseButton is null)
        {
            return;
        }

        var current = this.ResolveCurrentValue();

        if (this.decreaseButton is not null)
        {
            this.decreaseButton.IsEnabled = current > this.Minimum;
        }

        if (this.increaseButton is not null)
        {
            this.increaseButton.IsEnabled = current < this.Maximum;
        }
    }

    private static bool TryParseNumber(string? text, out int value) =>
        int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
