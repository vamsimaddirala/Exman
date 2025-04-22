using System.Globalization;

namespace Exman.Converters
{
    public class ItemTypeToIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Views.ApiTestPage.TreeItemType itemType)
            {
                return itemType switch
                {
                    Views.ApiTestPage.TreeItemType.Collection => "collection_icon.png",
                    Views.ApiTestPage.TreeItemType.Folder => "folder_icon.png",
                    Views.ApiTestPage.TreeItemType.Request => "request_icon.png",
                    _ => "default_icon.png"
                };
            }
            return "default_icon.png";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}