using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using OwnCloud.Extensions;
using OwnCloud.Data;
using Microsoft.Phone.Controls;

namespace OwnCloud.View.Controls
{
    public partial class CalendarDayOverview
    {
        public CalendarDayOverview()
        {
            InitializeComponent();
        }

        public DynamicCalendarSource EventSource
        {
            get;
            set;
        }

        private void CalendarDayOverview_OnLoaded(object sender, RoutedEventArgs e)
        {
            BackgroundGrid.SetGridRows(24);
            AppointmentGrid.SetGridRows(24);

            //Add horizontal lines
            for (int i = 0; i < 24; i++)
            {
                var r = new Rectangle
                {
                    Fill = new SolidColorBrush(Colors.White),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Height = 1
                };
                var tb = new TextBlock
                {
                    Text = i.ToString().PadLeft(2, '0') + ":00",
                    VerticalAlignment = VerticalAlignment.Top,
                    Style = Application.Current.Resources["PhoneTextNormalStyle"] as Style
                };
                Grid.SetRow(r, i);
                Grid.SetColumnSpan(r, 2);
                Grid.SetRow(tb, i);
                BackgroundGrid.Children.Add(r);
                BackgroundGrid.Children.Add(tb);
            }
            UpdateEvents();

        }

        public void UpdateEvents()
        {
            var events =
                EventSource.LoadEvents(this)
                           .Where(o => (o.To >= SelectedDate && o.From <= SelectedDate) || o.IsRecurringEvent)
                           .OrderByDescending(o => o.From - o.To).ToArray();

            var assorted = AssortTableEvents(events);
            PutEvents(assorted);
        }


        public void PutEvents(IEnumerable<TableEvent> events)
        {
            var nonFullDayEvents = events.Where(x => !x.IsFullDayEvent).ToArray();
            AppointmentGrid.SetGridColumns(nonFullDayEvents.Count());
            foreach (var currentEvent in events)
            {
                if (currentEvent.IsFullDayEvent)
                {
                    var tb = new TextBlock
                    {
                        Text = currentEvent.Title,
                        Foreground = GetCalendarColor(currentEvent.CalendarId),
                        Style = Application.Current.Resources["PhoneTextNormalStyle"] as Style,
                        Margin = new Thickness(0)
                    };
                    FullDayEvents.Children.Add(tb);
                }
                else
                {
                    int startRow = 0;
                    if (currentEvent.From.Date == SelectedDate ||
                        (currentEvent.IsRecurringEvent && IsRecuringEventStart(currentEvent, SelectedDate)))
                    {
                        startRow = currentEvent.From.Hour;
                    }

                    int endRow = 23;
                    if (currentEvent.To.Date == SelectedDate ||
                        (currentEvent.IsRecurringEvent && IsRecuringEventEnd(currentEvent, SelectedDate)))
                    {
                        endRow = Math.Max(currentEvent.To.Hour, 1);
                    }
                    var appointmentControl = new CalendarDayOverviewAppointment();
                    Grid.SetRow(appointmentControl, startRow);
                    Grid.SetRowSpan(appointmentControl, endRow - startRow + 1);
                    Grid.SetColumn(appointmentControl, nonFullDayEvents.ToList().IndexOf(currentEvent));
                    appointmentControl.DataContext = currentEvent;
                    appointmentControl.AppointmentBack.Fill = GetCalendarColor(currentEvent.CalendarId);
                    AppointmentGrid.Children.Add(appointmentControl);
                }
            }
        }

        public DateTime SelectedDate
        {
            get { return ((DateTime)DataContext).Date; }
        }

        private TableEvent[] AssortTableEvents(TableEvent[] events)
        {
            List<TableEvent> assorted = new List<TableEvent>();
            foreach (TableEvent tEvent in events)
            {
                if (tEvent.IsRecurringEvent)
                {
                    var rRules = OwnCloud.Data.Calendar.EventMetaUpdater.ParseRecurringRules(tEvent);
                    string rFrequency = (string)rRules.Single(r => r.Key == "FREQ").Value;
                    int rInterval = (int)rRules.Single(r => r.Key == "INTERVAL").Value;
                    DateTime rEnd = ((string)rRules.SingleOrDefault(r => r.Key == "UNTIL").Key == "UNTIL") ? Convert.ToDateTime(rRules.Single(r => r.Key == "UNTIL").Value) : DateTime.MaxValue;
                    int rCount = (string)rRules.SingleOrDefault(r => r.Key == "COUNT").Key == "COUNT" ? (int)rRules.Single(r => r.Key == "COUNT").Value : 0;

                    if (rCount == 0 && rEnd.Date < SelectedDate.Date) { continue; }
                    else if (rCount > 0)
                    {
                        for (var i = 0; i < rCount; i++)
                        {
                            switch (rFrequency)
                            {
                                case "DAILY":
                                    var currentDate = tEvent.From.Date.AddDays(i * rInterval).Date;
                                    var endDate = tEvent.IsFullDayEvent ? tEvent.To.Date.AddDays(i * rInterval).AddSeconds(-1).Date : tEvent.To.Date.AddDays(i * rInterval).Date;
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate.Date >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    break;
                                case "WEEKLY":
                                    currentDate = tEvent.From.Date.AddDays(i * rInterval * 7).Date;
                                    endDate = tEvent.IsFullDayEvent ? tEvent.To.Date.AddDays(i * rInterval * 7).AddSeconds(-1).Date : tEvent.To.Date.AddDays(i * rInterval * 7).Date;
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate.Date >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    break;
                                case "MONTHLY":
                                    currentDate = tEvent.From.Date.AddMonths(i * rInterval).Date;
                                    endDate = tEvent.IsFullDayEvent ? tEvent.To.Date.AddMonths(i * rInterval).AddSeconds(-1).Date : tEvent.To.Date.AddMonths(i * rInterval).Date;
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate.Date >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    break;
                                case "YEARLY":
                                    currentDate = tEvent.From.Date.AddYears(i * rInterval).Date;
                                    endDate = tEvent.IsFullDayEvent ? tEvent.To.Date.AddYears(i * rInterval).AddSeconds(-1).Date : tEvent.To.AddYears(i * rInterval).Date;
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate.AddYears(i * rInterval).Date >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    break;
                            }
                        }
                    }
                    else if (rEnd.Date >= SelectedDate)
                    {
                        var currentDate = tEvent.From.Date;
                        var endDate = tEvent.IsFullDayEvent ? tEvent.To.Date.AddSeconds(-1).Date : tEvent.To.Date;
                        switch (rFrequency)
                        {
                            case "DAILY":
                                while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                                {
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate.Date >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    currentDate = currentDate.AddDays(rInterval);
                                    endDate = endDate.AddDays(rInterval);
                                }
                                break;
                            case "WEEKLY":
                                while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                                {
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate.Date >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    currentDate = currentDate.AddDays(7 * rInterval);
                                    endDate = endDate.AddDays(7 + rInterval);
                                }
                                break;
                            case "MONTHLY":
                                while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                                {
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate.Date >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    currentDate = currentDate.AddMonths(rInterval);
                                    endDate = endDate.AddMonths(rInterval);
                                }
                                break;
                            case "YEARLY":
                                while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                                {
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    currentDate = currentDate.AddYears(rInterval);
                                    endDate = endDate.AddYears(rInterval);
                                }
                                break;
                        }
                    }
                    else if (rCount == 0 && rEnd == DateTime.MaxValue)
                    {
                        var currentDate = tEvent.From.Date;
                        var endDate = tEvent.IsFullDayEvent ? tEvent.To.Date.AddSeconds(-1).Date : tEvent.To.Date;
                        switch (rFrequency)
                        {
                            case "DAILY":
                                while (currentDate.Date <= SelectedDate.Date)
                                {
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    currentDate = currentDate.AddDays(rInterval);
                                    endDate = endDate.AddDays(rInterval);
                                }
                                break;
                            case "WEEKLY":
                                while (currentDate.Date <= SelectedDate.Date)
                                {
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    currentDate = currentDate.AddDays(7 * rInterval);
                                    endDate = endDate.AddDays(7 * rInterval);
                                }
                                break;
                            case "MONTHLY":
                                while (currentDate.Date <= SelectedDate.Date)
                                {
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate.Date >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    currentDate = currentDate.AddMonths(rInterval);
                                    endDate = endDate.AddMonths(rInterval);
                                }
                                break;
                            case "YEARLY":
                                while (currentDate.Date <= SelectedDate.Date)
                                {
                                    if (currentDate.Date <= SelectedDate.Date &&
                                        endDate.Date >= SelectedDate.Date)
                                    {
                                        assorted.Add(tEvent);
                                    }
                                    currentDate = currentDate.AddYears(rInterval);
                                    endDate = endDate.AddYears(rInterval);
                                }
                                break;
                        }
                    }
                }
                else
                {
                    assorted.Add(tEvent);
                }
            }
            return assorted.ToArray();
        }

        private bool IsRecuringEventStart(TableEvent tEvent, DateTime date)
        {
            var rRules = OwnCloud.Data.Calendar.EventMetaUpdater.ParseRecurringRules(tEvent);
            string rFrequency = (string)rRules.Single(r => r.Key == "FREQ").Value;
            int rInterval = (int)rRules.Single(r => r.Key == "INTERVAL").Value;
            DateTime rEnd = ((string)rRules.SingleOrDefault(r => r.Key == "UNTIL").Key == "UNTIL") ? Convert.ToDateTime(rRules.Single(r => r.Key == "UNTIL").Value) : DateTime.MaxValue;
            int rCount = (string)rRules.SingleOrDefault(r => r.Key == "COUNT").Key == "COUNT" ? (int)rRules.Single(r => r.Key == "COUNT").Value : 0;

            if (rCount > 0)
            {
                for (var i = 0; i < rCount; i++)
                {
                    switch (rFrequency)
                    {
                        case "DAILY":
                            if (tEvent.From.Date.AddDays(i * rInterval).Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            break;
                        case "WEEKLY":
                            if (tEvent.From.Date.AddDays(i * rInterval * 7).Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            break;
                        case "MONTHLY":
                            if (tEvent.From.Date.AddMonths(i * rInterval).Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            break;
                        case "YEARLY":
                            if (tEvent.From.Date.AddYears(i * rInterval).Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            break;
                    }
                }
            }
            else if (rEnd.Date >= SelectedDate)
            {
                switch (rFrequency)
                {
                    case "DAILY":
                        var currentDate = tEvent.From.Date;
                        while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddDays(rInterval);
                        }
                        break;
                    case "WEEKLY":
                        currentDate = tEvent.From.Date;
                        while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddDays(7 * rInterval);
                        }
                        break;
                    case "MONTHLY":
                        currentDate = tEvent.From.Date;
                        while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddMonths(rInterval);
                        }
                        break;
                    case "YEARLY":
                        currentDate = tEvent.From.Date;
                        while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddYears(rInterval);
                        }
                        break;
                }
            }
            else if (rCount == 0 && rEnd == DateTime.MaxValue)
            {
                switch (rFrequency)
                {
                    case "DAILY":
                        var currentDate = tEvent.From.Date;
                        while (currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddDays(rInterval);
                        }
                        break;
                    case "WEEKLY":
                        currentDate = tEvent.From.Date;
                        while (currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddDays(7 * rInterval);
                        }
                        break;
                    case "MONTHLY":
                        currentDate = tEvent.From.Date;
                        while (currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddMonths(rInterval);
                        }
                        break;
                    case "YEARLY":
                        currentDate = tEvent.From.Date;
                        while (currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddYears(rInterval);
                        }
                        break;
                }
            }
            return false;
        }

        private bool IsRecuringEventEnd(TableEvent tEvent, DateTime date)
        {
            var rRules = OwnCloud.Data.Calendar.EventMetaUpdater.ParseRecurringRules(tEvent);
            string rFrequency = (string)rRules.Single(r => r.Key == "FREQ").Value;
            int rInterval = (int)rRules.Single(r => r.Key == "INTERVAL").Value;
            DateTime rEnd = ((string)rRules.SingleOrDefault(r => r.Key == "UNTIL").Key == "UNTIL") ? Convert.ToDateTime(rRules.Single(r => r.Key == "UNTIL").Value) : DateTime.MaxValue;
            int rCount = (string)rRules.SingleOrDefault(r => r.Key == "COUNT").Key == "COUNT" ? (int)rRules.Single(r => r.Key == "COUNT").Value : 0;

            if (rCount > 0)
            {
                for (var i = 0; i < rCount; i++)
                {
                    switch (rFrequency)
                    {
                        case "DAILY":
                            if (tEvent.To.Date.AddDays(i * rInterval).Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            break;
                        case "WEEKLY":
                            if (tEvent.To.Date.AddDays(i * rInterval * 7).Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            break;
                        case "MONTHLY":
                            if (tEvent.To.Date.AddMonths(i * rInterval).Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            break;
                        case "YEARLY":
                            if (tEvent.To.Date.AddYears(i * rInterval).Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            break;
                    }
                }
            }
            else if (rEnd.Date >= SelectedDate)
            {
                var currentDate = tEvent.To.Date;
                switch (rFrequency)
                {
                    case "DAILY":
                        while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddDays(rInterval);
                        }
                        break;
                    case "WEEKLY":
                        while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddDays(7 * rInterval);
                        }
                        break;
                    case "MONTHLY":
                        while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddMonths(rInterval);
                        }
                        break;
                    case "YEARLY":
                        while (currentDate <= rEnd && currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddYears(rInterval);
                        }
                        break;
                }
            }
            else if (rCount == 0 && rEnd == DateTime.MaxValue)
            {
                var currentDate = tEvent.To.Date;
                switch (rFrequency)
                {
                    case "DAILY":
                        while (currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddDays(rInterval);
                        }
                        break;
                    case "WEEKLY":
                        while (currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddDays(7 * rInterval);
                        }
                        break;
                    case "MONTHLY":
                        while (currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddMonths(rInterval);
                        }
                        break;
                    case "YEARLY":
                        while (currentDate.Date <= SelectedDate.Date)
                        {
                            if (currentDate.Date == SelectedDate.Date)
                            {
                                return true;
                            }
                            currentDate = currentDate.AddYears(rInterval);
                        }
                        break;
                }
            }
            return false;
        }

        private SolidColorBrush GetCalendarColor(int CalendarId)
        {
            var calendar = App.DataContext.Calendars.Where(o => o.Id == CalendarId).FirstOrDefault();
            var converter = new OwnCloud.View.Converter.CalendarColorConverter();
            SolidColorBrush color = (SolidColorBrush)converter.Convert(calendar.CalendarColor, null, null, null);
            return color;
        }
    }
}