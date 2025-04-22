using System.Globalization;
using Microsoft.Maui.Controls;

namespace Exman.Converters
{
    public class TabToStyleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string currentTab && parameter is string targetTab)
            {
                var appResources = Application.Current?.Resources;
                return currentTab == targetTab 
                    ? appResources?["ActiveTabButton"] ?? new Style(typeof(Button))
                    : appResources?["TabButton"] ?? new Style(typeof(Button));
            }
            
            return Application.Current?.Resources["TabButton"] ?? new Style(typeof(Button));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}