using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace Nextcloud.Converter
{
    class InverseBooleanConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language) {
            if(targetType != typeof(bool)) {
                throw new InvalidOperationException("The target must be of type bool");
            } else {
                return !(bool)value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotSupportedException();
        }
    }
}
