using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Nextcloud.Converter
{
    class FilenameRootdirConverter : DependencyObject, IValueConverter
    {
        public string RootDir
        {
            get { return (string)GetValue(RootDirProperty); }
            set { SetValue(RootDirProperty, value); }
        }

        public static readonly DependencyProperty RootDirProperty = DependencyProperty.Register("RootDir", typeof(string), typeof(FilenameRootdirConverter), new PropertyMetadata(""));

        public object Convert(object value, Type targetType, object parameter, string language) {
            var itemPath = value.ToString();
            if(RootDir.TrimEnd('/').EndsWith(itemPath)) {
                return "..";
            } else {
                return itemPath;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotSupportedException();
        }
    }
}
