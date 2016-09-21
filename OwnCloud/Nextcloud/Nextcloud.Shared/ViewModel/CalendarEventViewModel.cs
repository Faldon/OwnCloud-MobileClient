﻿using Nextcloud.Data;
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
                    return calendarEvent.StartDate.ToLocalTime().FormatDate("hour minute");
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
                    var duration = (calendarEvent.EndDate - calendarEvent.StartDate).Duration().TotalHours.ToString() + "h";
                    if (calendarEvent.Location.Length != 0) {
                        duration += " (" + calendarEvent.Location + ")";
                    }
                    return duration;
                } else {
                    return "";
                }
            }
        }

        public string CalendarColor
        {
            get { return calendarEvent.CalendarObject.Calendar.Color; }
        }

        public string Location
        {
            get { return calendarEvent.Location; }
        }

        public Brush FullDayEventIndicatorStroke
        {
            get { return GetCalendarColor(); }
        }

        public Brush FullDayEventIndicatorFill
        {
            get
            {
                if(!calendarEvent.IsFullDayEvent) {
                    return GetCalendarColor();
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
                    return GetCalendarColor();
                } else {
                    return new SolidColorBrush(Colors.White);
                }
            }
        }

        public double RowSpan
        {
            get { return calendarEvent.IsFullDayEvent ? 2 : 1; }
        }

        public CalendarEventViewModel(CalendarEvent dataModel) {
            calendarEvent = dataModel;
        }

        private Brush GetCalendarColor() {
            var converter = new HexcodeColorConverter();
            SolidColorBrush color = (SolidColorBrush)converter.Convert(CalendarColor, null, null, null);
            return color;
        }
    }
}
