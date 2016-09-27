using Nextcloud.Data;
using Nextcloud.Shared.Converter;
using Nextcloud.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace Nextcloud.ViewModel
{
    class CalendarEventViewModel : ViewModel
    {
        private CalendarEvent calendarEvent;

        public string CalendarEventId
        {
            get { return calendarEvent.CalendarEventId.ToString();  }
        }

        public string Summary
        {
            get { return calendarEvent.Summary; }
        }

        public string StartTime
        {
            get
            {
                if(!calendarEvent.IsFullDayEvent) {
                    return calendarEvent.StartDate.ToLocalTime().FormatDate("{hour.integer(2)}:{minute.integer(2)} {period.abbreviated}");
                } else {
                    return "";
                }
            }
        }

        public string Duration
        {
            get
            {
                if (!calendarEvent.IsFullDayEvent) {
                    var duration = calendarEvent.Duration ?? (calendarEvent.EndDate - calendarEvent.StartDate).Duration().TotalHours.ToString() + "h";
                    if (calendarEvent.Location.Length != 0) {
                        duration += " (" + calendarEvent.Location.ToString() + ")";
                    }
                    return duration;
                } else {
                    return "";
                }
            }
        }

        public Brush CalendarColor
        {
            get { return calendarEvent.CalendarObject.Calendar.GetColor(); }
        }

        public string Location
        {
            get { return calendarEvent.Location; }
        }

        public Brush FullDayEventIndicatorFill
        {
            get
            {
                if(!calendarEvent.IsFullDayEvent) {
                    return calendarEvent.CalendarObject.Calendar.GetColor();
                } else {
                    return new SolidColorBrush(Colors.Black);
                }
            }
        }

        public Brush SummaryForeground
        {
            get
            {
                if (calendarEvent.IsFullDayEvent) {
                    return calendarEvent.CalendarObject.Calendar.GetColor();
                } else {
                    return new SolidColorBrush(Colors.White);
                }
            }
        }

        public double RowSpan
        {
            get { return calendarEvent.IsFullDayEvent ? 2 : 1; }
        }

        public bool IsDurationVisible
        {
            get { return !calendarEvent.IsFullDayEvent; }
        }

        public CalendarEventViewModel(CalendarEvent dataModel) {
            calendarEvent = dataModel;
        }
    }
}
