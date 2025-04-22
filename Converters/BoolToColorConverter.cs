using System.Globalization;

namespace Exman.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colors)
            {
                var colorParts = colors.Split(',');
                if (colorParts.Length == 2)
                {
                    string colorValue = boolValue ? colorParts[0] : colorParts[1];
                    return Color.FromArgb(colorValue.Trim());
                }
            }
            
            return Color.FromArgb("#505050");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}