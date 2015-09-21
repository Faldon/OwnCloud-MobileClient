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
    public class DateToWeekOfYearConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var date = (DateTime)value;
            var cal = new GregorianCalendar(GregorianCalendarTypes.Localized);
            return String.Format(Resource.Localization.AppResources.CalendarDayOverview_WeekOfYear, cal.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new DateTime();
        }
    }
}
