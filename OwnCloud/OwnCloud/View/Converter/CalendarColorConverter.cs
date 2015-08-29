using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace OwnCloud.View.Converter
{
    public class CalendarColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return new SolidColorBrush((Color)Application.Current.Resources["PhoneAccentColor"]);
            }
            string hexcolor = (string)value;
            if (hexcolor.Length == 0)
            {
                return new SolidColorBrush((Color)Application.Current.Resources["PhoneAccentColor"]);
            }
            byte alpha = Byte.Parse("FF", NumberStyles.HexNumber);
            byte red = Byte.Parse(hexcolor.Substring(1, 2), NumberStyles.HexNumber);
            byte green = Byte.Parse(hexcolor.Substring(3, 2), NumberStyles.HexNumber);
            byte blue = Byte.Parse(hexcolor.Substring(5, 2), NumberStyles.HexNumber);

            return new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush color = (SolidColorBrush)value;
            string hexcolor = "#" + color.Color.R.ToString("x") + color.Color.G.ToString("x") + color.Color.B.ToString("x");
            return hexcolor;
        }
    }
}
