using System;
using Windows.UI.Xaml.Data;

namespace Nextcloud.Converter
{
    public class ToUppercaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value == null)
            {
                return "";
            }

            return value.ToString().ToUpper();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }
}
