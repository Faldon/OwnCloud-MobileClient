using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Nextcloud.Converter
{
    class FileModifiedDateVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var visibleFilename = value.ToString();
            if (visibleFilename=="..") {
                return Visibility.Collapsed;
            } else {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotSupportedException();
        }
    }
}
