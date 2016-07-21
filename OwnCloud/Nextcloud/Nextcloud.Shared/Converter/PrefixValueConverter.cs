using System;
using Windows.UI.Xaml.Data;

namespace Nextcloud.Converter
{
    public class PrefixValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) {
            string input = value.ToString();
            int prefixLength;
            if (!int.TryParse(parameter.ToString(), out prefixLength) ||input.Length <= prefixLength) {
                return input;
            } else {
                return input.Substring(0, prefixLength).ToUpper();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotSupportedException();
        }
    }
}
