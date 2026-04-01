using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Spexts.Helpers;

/// <summary>
/// Converts a hex color string (e.g. "#3FB950") to a SolidColorBrush.
/// If null or empty, returns the default theme text color (#E6EDF3).
/// </summary>
public class NullToDefaultColorConverter : IValueConverter
{
    private static readonly SolidColorBrush DefaultBrush =
        new(Color.FromRgb(0xE6, 0xED, 0xF3));

    static NullToDefaultColorConverter()
    {
        DefaultBrush.Freeze();
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }
            catch
            {
                return DefaultBrush;
            }
        }
        return DefaultBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
