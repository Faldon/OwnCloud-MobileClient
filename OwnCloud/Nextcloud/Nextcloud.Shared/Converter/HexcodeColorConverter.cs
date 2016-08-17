using System;
using System.Globalization;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Nextcloud.Shared.Converter
{
    public class HexcodeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value == null)
            {
                
                throw new ArgumentNullException();
            }
            string hexcolor = (string)value;
            if (hexcolor.Length != 7)
            {
                throw new ArgumentException("Invalid lenght of supplied hexcolor");
            }
            byte alpha = Byte.Parse("FF", NumberStyles.HexNumber);
            byte red = Byte.Parse(hexcolor.Substring(1, 2), NumberStyles.HexNumber);
            byte green = Byte.Parse(hexcolor.Substring(3, 2), NumberStyles.HexNumber);
            byte blue = Byte.Parse(hexcolor.Substring(5, 2), NumberStyles.HexNumber);

            return new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            SolidColorBrush color = (SolidColorBrush)value;
            string hexcolor = "#" + color.Color.R.ToString("x") + color.Color.G.ToString("x") + color.Color.B.ToString("x");
            return hexcolor;
        }
    }
}
