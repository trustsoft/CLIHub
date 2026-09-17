using System.Globalization;
using System.Windows.Data;

namespace CLIHub.App.Views;

/// <summary>
/// Left-trims a long folder path so the meaningful tail (the folder name)
/// stays visible, matching the mockup's "...tail" presentation.
/// </summary>
public sealed class PathLeftTrimConverter : IValueConverter
{
    private const int MaxLength = 33;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || path.Length <= MaxLength)
        {
            return value ?? string.Empty;
        }

        return "..." + path[^(MaxLength - 3)..];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
