using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Nextcloud.Converter
{
    class FilenameRootdirConverter : DependencyObject, IValueConverter
    {
        public bool IsRoot
        {
            get { return (bool)GetValue(IsRootProperty); }
            set { SetValue(IsRootProperty, value); }
        }

        public static readonly DependencyProperty IsRootProperty = DependencyProperty.Register("IsRoot", typeof(bool), typeof(FilenameRootdirConverter), new PropertyMetadata(false));

        public object Convert(object value, Type targetType, object parameter, string language) {
            var rootItem = GetValue(IsRootProperty) as bool?;
            if (rootItem?? false) {
                return "..";
            } else {
                return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotSupportedException();
        }
    }
}
