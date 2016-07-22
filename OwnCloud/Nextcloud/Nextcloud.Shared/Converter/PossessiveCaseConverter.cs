using System;
using Windows.UI.Xaml.Data;

namespace Nextcloud.Converter
{
    public class PossessiveCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language) {
            string delim = App.Localization().GetString("PossessiveCaseDelimiter");
            string genitiveSymbol = App.Localization().GetString("GenitiveSymbol");
            string input = value.ToString();

            if(input.EndsWith(genitiveSymbol, StringComparison.CurrentCultureIgnoreCase)) {
                return input + delim;
            } else {
                return input + delim + genitiveSymbol;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            string delim = App.Localization().GetString("PossessiveCaseDelimiter");
            string genitiveSymbol = App.Localization().GetString("GenitiveSymbol");
            string input = value.ToString();

            if(input.LastIndexOf(genitiveSymbol)>input.LastIndexOf(delim)) {
                return input.Remove(input.LastIndexOf(genitiveSymbol), genitiveSymbol.Length).Remove(input.LastIndexOf(delim), delim.Length);
            } else {
                return input.Remove(input.LastIndexOf(delim), delim.Length);
            }
        }
    }
}
