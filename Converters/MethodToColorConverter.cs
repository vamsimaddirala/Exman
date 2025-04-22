using System.Globalization;

namespace Exman.Converters
{
    public class MethodToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string method)
            {
                return method.ToUpperInvariant() switch
                {
                    "GET" => Color.FromArgb("#0D6E6F"),
                    "POST" => Color.FromArgb("#008000"),
                    "PUT" => Color.FromArgb("#9B4700"),
                    "DELETE" => Color.FromArgb("#CC0000"),
                    "PATCH" => Color.FromArgb("#9932CC"),
                    "HEAD" => Color.FromArgb("#5C6BC0"),
                    "OPTIONS" => Color.FromArgb("#795548"),
                    _ => Color.FromArgb("#505050")
                };
            }
            return Color.FromArgb("#505050");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}