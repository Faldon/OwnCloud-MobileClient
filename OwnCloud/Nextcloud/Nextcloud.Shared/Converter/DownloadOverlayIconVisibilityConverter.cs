using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Nextcloud.Converter
{
    class DownloadOverlayIconVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if((bool)value) {
                return Visibility.Visible;
            } else {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotSupportedException();
        }
    }
}
