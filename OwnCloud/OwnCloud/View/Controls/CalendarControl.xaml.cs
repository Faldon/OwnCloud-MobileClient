using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Ocwp.Controls;
using OwnCloud.Data;
using OwnCloud.Extensions;
using System.Windows.Media.Animation;

namespace OwnCloud.View.Controls
{
    public partial class CalendarControl
    {
        static Dictionary<string, string> plurals = new Dictionary<string, string>() {
                           {"en" , "s"},
                           {"de" , "n"},
                           {"fr" , "s"}
                        };

        public CalendarControl()
        {
            InitializeComponent();

            Unloaded += CalendarControl_Unloaded;
        }

        void CalendarControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_context != null)
            {
                Context.Dispose();
                _context = null;
            }
        }


        #region private Fields

        private int _weekCount = 0;
        private Data.OwnCloudDataContext _context;
        private Data.OwnCloudDataContext Context
        {
            get { return _context ?? (_context = new OwnCloudDataContext()); }
        }
        private DateTime _firstDayOfCalendarMonth;
        private DateTime _lastDayOfCalendarMonth;
        private Dictionary<int, StackPanel> _dayPanels = new Dictionary<int, StackPanel>();

        #endregion

        #region Public Properties

        private int? _accountID;
        public int? AccountID
        {
            get { return _accountID; }
            set
            {
                if (_accountID == null)
                {
                    _accountID = value;
                }
                else
                    _accountID = value;
            }
        }


        public DateTime SelectedDate
        {
            get { return (DateTime)GetValue(SelectedDateProperty); }
            set
            {
                SetValue(SelectedDateProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for SelectedDate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register("SelectedDate", typeof(DateTime), typeof(CalendarControl), new PropertyMetadata(DateTime.MinValue, OnSelectedDateChaged));

        private static void OnSelectedDateChaged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as CalendarControl).SelectedDateChanged(e);
        }

        private void SelectedDateChanged(DependencyPropertyChangedEventArgs e)
        {
            OnDateChanging();

            if ((DateTime)e.NewValue > (DateTime)e.OldValue)
                SlideLeftBegin.Begin();
            else SlideRightBegin.Begin();
        }



        #endregion

        #region private Functions

        private void SlideLeftBegin_OnCompleted(object sender, EventArgs e)
        {
            ChangeDate();
            SlideLeftEnd.Begin();
        }

        private void SlideRightBegin_OnCompleted(object sender, EventArgs e)
        {
            ChangeDate();
            SlideRightEnd.Begin();
        }

        public void ChangeDate()
        {
            OnDateChanged();

            _firstDayOfCalendarMonth = SelectedDate.FirstOfMonth().FirstDayOfWeek().Date;
            _lastDayOfCalendarMonth = SelectedDate.LastOfMonth().LastDayOfWeek().AddDays(1);

            _weekCount = SelectedDate.GetMonthCount();
            ResetGridLines();

            if (_accountID == null)
                return;

            Dispatcher.BeginInvoke(new Action(() => RefreshAppointments()));
        }

        private void ResetGridLines()
        {
            GrdCalendarLines.Children.Clear();
            GrdCalendarLines.SetGridRows(_weekCount + 1);
            GrdCalendarLines.SetGridColumns(7);

            GrdDayIndicator.Children.Clear();
            GrdDayIndicator.SetGridRows(_weekCount + 1);
            GrdDayIndicator.SetGridColumns(7);

            GrdAppointments.Children.Clear();
            GrdAppointments.SetGridRows(_weekCount + 1);
            GrdAppointments.SetGridColumns(7);

            var firstDay = SelectedDate.FirstOfMonth().FirstDayOfWeek();
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < _weekCount; j++)
                {
                    DateTime fieldDate = firstDay.AddDays((j * 7) + i);

                    Color dayIndicatorColor = Colors.White;
                    if (fieldDate.Date == DateTime.Now.Date)
                    {
                        dayIndicatorColor = (Color)Application.Current.Resources["PhoneAccentColor"];
                    }
                    else if (fieldDate.Month != SelectedDate.Month)
                    {
                        dayIndicatorColor = Colors.Gray;
                    }

                    var dayIndicator = new TextBlock
                        {
                            VerticalAlignment = VerticalAlignment.Bottom,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Text = fieldDate.Day.ToString(CultureInfo.InvariantCulture),
                            Foreground = new SolidColorBrush(dayIndicatorColor),
                            Margin = new Thickness(5, 0, 0, 5)
                        };
                    Grid.SetColumn(dayIndicator, i);
                    Grid.SetRow(dayIndicator, j + 1);
                    GrdDayIndicator.Children.Add(dayIndicator);

                    var dayOpenControl = new StackPanel
                        {
                            Background = new SolidColorBrush(Colors.Black),
                            Name = fieldDate.ToString()
                        };
                    dayOpenControl.Tap += ShowDayDetails;
                    Grid.SetColumn(dayOpenControl, i);
                    Grid.SetRow(dayOpenControl, j + 1);
                    GrdCalendarLines.Children.Add(dayOpenControl);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                var vRect = new Rectangle();
                vRect.Fill = new SolidColorBrush(Colors.White);
                vRect.Width = 2;
                vRect.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetRow(vRect, 1);
                Grid.SetRowSpan(vRect, _weekCount);
                Grid.SetColumn(vRect, i);

                GrdCalendarLines.Children.Add(vRect);
            }
            for (int i = 0; i < _weekCount + 1; i++)
            {
                var hRect = new Rectangle();
                hRect.Fill = new SolidColorBrush(Colors.White);
                hRect.Height = 2;
                hRect.VerticalAlignment = VerticalAlignment.Bottom;
                Grid.SetColumnSpan(hRect, 7);
                Grid.SetRow(hRect, i);

                GrdCalendarLines.Children.Add(hRect);
            }

            var targetDate = _firstDayOfCalendarMonth;
            for (int i = 0; i < 7; i++)
            {

                TextBlock dayBlock = new TextBlock();
                Grid.SetColumn(dayBlock, i);
                dayBlock.VerticalAlignment = VerticalAlignment.Bottom;
                dayBlock.HorizontalAlignment = HorizontalAlignment.Center;
                dayBlock.Text = targetDate.ToString("ddd");
                GrdCalendarLines.Children.Add(dayBlock);

                targetDate = targetDate.AddDays(1);
            }


        }

        /// <summary>
        /// Aktualisiert die Termine für den Kalendar im angegebenen Monat
        /// </summary>
        public void RefreshAppointments()
        {
            var calendarEvents = Context.Calendars.Where(o =>
                o._accountId == AccountID).Select(o =>
                    o.Events.Where(q =>
                        (q.To > _firstDayOfCalendarMonth && q.From < _lastDayOfCalendarMonth) || ((q.IsRecurringEvent) && (q.From < _lastDayOfCalendarMonth))
                    )
                );

            //merge all calendar events
            IEnumerable<TableEvent> events = new TableEvent[0];

            foreach (var calendar in calendarEvents)
                events = events.Concat(calendar);

            events = events
                .OrderByDescending(o => o.To - o.From) //Längere Event sollen oben angezeigt werden
                .ToArray();


            //Refresh events to get the changes, if a sync was completed
            Context.Refresh(RefreshMode.OverwriteCurrentValues, events);

            //Delete displayed events
            GrdAppointments.Children.Clear();
            _dayPanels.Clear();

            //INsert new events
            PutEvents(events);
        }

        /// <summary>
        /// Puts the events into the control
        /// </summary>
        /// <param name="events"></param>
        private void PutEvents(IEnumerable<TableEvent> events)
        {
            foreach (var tableEvent in events)
            {
                var currentDate = tableEvent.From.Date;
                var endDate = tableEvent.To.Date;

                if (tableEvent.IsRecurringEvent)
                {
                    var rRules = OwnCloud.Data.Calendar.EventMetaUpdater.ParseRecurringRules(tableEvent);
                    string rFrequency = (string)rRules.Single(r => r.Key == "FREQ").Value;
                    int rInterval = (int)rRules.Single(r => r.Key == "INTERVAL").Value;
                    DateTime rEnd = ((string)rRules.SingleOrDefault(r => r.Key == "UNTIL").Key == "UNTIL") ? Convert.ToDateTime(rRules.Single(r => r.Key == "UNTIL").Value) : DateTime.MaxValue;
                    int rCount = (string)rRules.SingleOrDefault(r => r.Key == "COUNT").Key == "COUNT" ? (int)rRules.Single(r => r.Key == "COUNT").Value : 0;

                    if (rCount == 0 && rEnd < _firstDayOfCalendarMonth) { continue; }
                    else if (rCount > 0)
                    {
                        for (var i = 0; i < rCount; i++)
                        {
                            switch (rFrequency)
                            {
                                case "DAILY":
                                    currentDate = tableEvent.From.Date.AddDays(i * rInterval);
                                    endDate = tableEvent.To.Date.AddDays(i * rInterval);
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                    break;
                                case "WEEKLY":
                                    currentDate = tableEvent.From.Date.AddDays(i * rInterval * 7);
                                    endDate = tableEvent.To.Date.AddDays(i * rInterval * 7);
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                    break;
                                case "MONTHLY":
                                    currentDate = tableEvent.From.Date.AddMonths(i * rInterval);
                                    endDate = tableEvent.To.Date.AddMonths(i * rInterval);
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                    break;
                                case "YEARLY":
                                    currentDate = tableEvent.From.Date.AddYears(i * rInterval);
                                    endDate = tableEvent.To.Date.AddYears(i * rInterval);
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                    break;
                            }
                        }
                    }
                    else if (rEnd > _firstDayOfCalendarMonth)
                    {
                        switch (rFrequency)
                        {
                            case "DAILY":
                                currentDate = currentDate.AddDays((_firstDayOfCalendarMonth - currentDate).TotalDays + (_firstDayOfCalendarMonth - currentDate).TotalDays % rInterval);
                                endDate = endDate.AddDays((_firstDayOfCalendarMonth - endDate).TotalDays + (_firstDayOfCalendarMonth - endDate).TotalDays % rInterval);
                                while (currentDate < tableEvent.From)
                                {
                                    currentDate = currentDate.AddDays(rInterval);
                                    endDate = endDate.AddDays(rInterval);
                                }
                                while (currentDate <= rEnd && currentDate <= _lastDayOfCalendarMonth)
                                {
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                    currentDate = currentDate.AddDays(rInterval);
                                    endDate = endDate.AddDays(rInterval);
                                }
                                break;
                            case "WEEKLY":
                                currentDate = tableEvent.From;
                                endDate = tableEvent.To;
                                while (currentDate <= _firstDayOfCalendarMonth)
                                {
                                    currentDate = currentDate.AddDays(7 * rInterval);
                                    endDate = endDate.AddDays(7 * rInterval);
                                }
                                while (currentDate <= rEnd && currentDate <= _lastDayOfCalendarMonth)
                                {
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                    currentDate = currentDate.AddDays(7 * rInterval);
                                    endDate = endDate.AddDays(7 * rInterval);
                                }
                                break;
                            case "MONTHLY":
                                currentDate = tableEvent.From;
                                endDate = tableEvent.To;
                                while (currentDate.Date < SelectedDate.Date)
                                {
                                    currentDate = currentDate.AddMonths(rInterval);
                                    endDate = endDate.AddMonths(rInterval);
                                }
                                if (currentDate <= rEnd && currentDate >= _firstDayOfCalendarMonth && currentDate <= _lastDayOfCalendarMonth)
                                {
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                }
                                break;
                            case "YEARLY":
                                currentDate = tableEvent.From;
                                endDate = tableEvent.To;
                                while (currentDate.Year < SelectedDate.Year)
                                {
                                    currentDate = currentDate.AddYears(rInterval);
                                    endDate = endDate.AddYears(rInterval);
                                }
                                if (currentDate <= rEnd && currentDate >= _firstDayOfCalendarMonth && currentDate <= _lastDayOfCalendarMonth)
                                {
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                }
                                break;
                        }
                    }
                    else if (rCount == 0 && rEnd == DateTime.MaxValue)
                    {
                        switch (rFrequency)
                        {
                            case "DAILY":
                                currentDate = currentDate.AddDays((_firstDayOfCalendarMonth - currentDate).TotalDays + (_firstDayOfCalendarMonth - currentDate).TotalDays % rInterval);
                                endDate = endDate.AddDays((_firstDayOfCalendarMonth - endDate).TotalDays + (_firstDayOfCalendarMonth - endDate).TotalDays % rInterval);
                                while (currentDate < tableEvent.From)
                                {
                                    currentDate = currentDate.AddDays(rInterval);
                                    endDate = endDate.AddDays(rInterval);
                                }
                                while (currentDate <= _lastDayOfCalendarMonth)
                                {
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                    currentDate = currentDate.AddDays(rInterval);
                                    endDate = endDate.AddDays(rInterval);
                                }
                                break;
                            case "WEEKLY":
                                currentDate = tableEvent.From;
                                endDate = tableEvent.To;
                                while (currentDate < _firstDayOfCalendarMonth)
                                {
                                    currentDate = currentDate.AddDays(7 * rInterval);
                                    endDate = endDate.AddDays(7 * rInterval);
                                }
                                while (currentDate <= _lastDayOfCalendarMonth)
                                {
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                    currentDate = currentDate.AddDays(7 * rInterval);
                                    endDate = endDate.AddDays(7 * rInterval);
                                }
                                break;
                            case "MONTHLY":
                                while (currentDate.Date < SelectedDate.Date)
                                {
                                    currentDate = currentDate.AddMonths(rInterval);
                                    endDate = endDate.AddMonths(rInterval);
                                }
                                if (currentDate >= _firstDayOfCalendarMonth && currentDate <= _lastDayOfCalendarMonth)
                                {
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                }
                                break;
                            case "YEARLY":
                                currentDate = tableEvent.From;
                                endDate = tableEvent.To;
                                while (currentDate.Year < SelectedDate.Year)
                                {
                                    currentDate = currentDate.AddYears(rInterval);
                                    endDate = endDate.AddYears(rInterval);
                                }
                                if (currentDate >= _firstDayOfCalendarMonth && currentDate <= _lastDayOfCalendarMonth)
                                {
                                    PutSingleEvent(tableEvent, currentDate, endDate);
                                }
                                break;
                        }
                    }
                }

                else
                {
                    PutSingleEvent(tableEvent, currentDate, endDate);
                }
            }
        }

        private void PutSingleEvent(TableEvent tableEvent, DateTime currentDate, DateTime endDate)
        {
            if (endDate == currentDate)
                endDate = endDate.AddSeconds(1);

            while (currentDate < endDate)
            {
                StackPanel dPanel = GetDayStackPanel(currentDate);

                if (dPanel == null) { currentDate = currentDate.AddDays(1); continue; }

                var rect = new Rectangle
                {
                    Name = tableEvent.EventId.ToString() + "_" + currentDate.ToShortDateString(),
                    Fill = GetCalendarColor(tableEvent.CalendarId),
                    Width = GrdAppointments.ColumnDefinitions.FirstOrDefault().ActualWidth * 0.3,
                    Height = 5,
                    Margin = new Thickness(0, 5, 10, 0),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right
                };
                dPanel.Children.Add(rect);
                currentDate = currentDate.AddDays(1);
            }

        }

        void ShowDayDetails(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StackPanel dPanel = (StackPanel)sender;
            DateTime fieldDate = DateTime.Parse(dPanel.Name);
            StackPanel dsp = GetDayStackPanel(fieldDate);

            List<int> dayEvents = new List<int>();
            foreach (Rectangle rect in dsp.Children)
            {
                dayEvents.Add(Int32.Parse(rect.Name.Substring(0, rect.Name.IndexOf('_'))));
            }
            IEnumerable<TableEvent> events = GetCalendarEvents(dayEvents);

            if (DayDetailsHeader.Text == fieldDate.ToLongDateString())
            {
                DayDetails.Visibility = DayDetails.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                RefreshAppointments();
                return;
            }

            DayDetails.Margin = new Thickness(0, GrdAppointments.RowDefinitions.FirstOrDefault().ActualHeight * 2, 0, GrdAppointments.RowDefinitions.LastOrDefault().ActualHeight + 2);
            DayDetailsHeader.Text = fieldDate.ToLongDateString();

            DayAppointmentDetails.Children.Clear();
            foreach (TableEvent tableEvent in events)
            {
                Grid dayEvent = new Grid()
                    {
                        Name = tableEvent.EventId.ToString()
                    };
                switch (tableEvent.IsFullDayEvent)
                {
                    case true:
                        dayEvent.SetGridRows(1);
                        dayEvent.SetGridColumns(2);
                        dayEvent.ColumnDefinitions[0].Width = new GridLength(20);

                        TextBlock fullDayText = new TextBlock()
                        {
                            Text = tableEvent.Title,
                            Style = Application.Current.Resources["PhoneTextSmallStyle"] as Style,
                            Foreground = GetCalendarColor(tableEvent.CalendarId),
                            Margin = new Thickness(10, 0, 10, 0)
                        };
                        Grid.SetColumn(fullDayText, 1);
                        Grid.SetRow(fullDayText, 0);

                        var fullDayRect = new Rectangle()
                        {
                            StrokeThickness = 1.0,
                            Stroke = GetCalendarColor(tableEvent.CalendarId),
                            Fill = new SolidColorBrush(Colors.Black),
                            Height = fullDayText.ActualHeight * 0.8,
                            Width = 10,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(fullDayRect, 0);
                        Grid.SetRow(fullDayRect, 0);

                        dayEvent.Children.Add(fullDayRect);
                        dayEvent.Children.Add(fullDayText);
                        break;
                    case false:
                        dayEvent.SetGridRows(2);
                        dayEvent.SetGridColumns(3);
                        dayEvent.ColumnDefinitions[0].Width = new GridLength(20);
                        dayEvent.ColumnDefinitions[1].Width = GridLength.Auto;

                        TextBlock startTime = new TextBlock()
                        {
                            Text = tableEvent.From.ToString("HH:mm"),
                            Style = Application.Current.Resources["PhoneTextExtraSmallStyle"] as Style,
                            Foreground = new SolidColorBrush(Colors.White),
                            VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                            Margin = new Thickness(10, 0, 10, 0)
                        };
                        Grid.SetColumn(startTime, 1);
                        Grid.SetRow(startTime, 0);

                        TextBlock dayText = new TextBlock()
                        {
                            Text = tableEvent.Title,
                            Style = Application.Current.Resources["PhoneTextExtraSmallStyle"] as Style,
                            Foreground = GetCalendarColor(tableEvent.CalendarId),
                            VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                            Margin = new Thickness(10, 0, 10, 0)
                        };
                        Grid.SetColumn(dayText, 2);
                        Grid.SetRow(dayText, 0);

                        string[] param = { 
                                             (tableEvent.To - tableEvent.From).Hours.ToString(), 
                                             (tableEvent.To - tableEvent.From).Hours == 1 ? "" : CalendarControl.plurals[CultureInfo.CurrentCulture.TwoLetterISOLanguageName], 
                                             (tableEvent.To - tableEvent.From).Minutes.ToString(),
                                             (tableEvent.To - tableEvent.From).Minutes == 1 ? "" : CalendarControl.plurals[CultureInfo.CurrentCulture.TwoLetterISOLanguageName], 
                                         };
                        TextBlock durationText = new TextBlock()
                        {
                            Text = String.Format(Resource.Localization.AppResources.AppointmentDuration, param),
                            Style = Application.Current.Resources["PhoneTextExtraSmallStyle"] as Style,
                            Foreground = new SolidColorBrush(Colors.Gray),
                            VerticalAlignment = System.Windows.VerticalAlignment.Top,
                            Margin = new Thickness(10, 0, 10, 0)
                        };
                        string loc = OwnCloud.Data.Calendar.Parsing.ParserICal.ParseLocation(tableEvent);
                        if (loc.Length != 0)
                        {
                            durationText.Text += " (" + loc + ")";
                        }
                        Grid.SetColumn(durationText, 2);
                        Grid.SetRow(durationText, 1);

                        var dayRect = new Rectangle()
                        {
                            StrokeThickness = 1.0,
                            Stroke = GetCalendarColor(tableEvent.CalendarId),
                            Fill = GetCalendarColor(tableEvent.CalendarId),
                            Height = dayText.ActualHeight * 1.7,
                            Width = 10,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(dayRect, 0);
                        Grid.SetRow(dayRect, 0);
                        Grid.SetRowSpan(dayRect, 2);

                        dayEvent.Children.Add(dayRect);
                        dayEvent.Children.Add(startTime);
                        dayEvent.Children.Add(dayText);
                        dayEvent.Children.Add(durationText);
                        break;
                }
                dayEvent.Tap += OpenAppointmentPage;
                DayAppointmentDetails.Children.Add(dayEvent);
            }
            DayDetails.Visibility = Visibility.Visible;
        }

        void OpenAppointmentPage(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Grid dayEvent = (Grid)sender;     
            App.Current.RootFrame.Navigate(new Uri("/View/Page/AppointmentDetailPage.xaml?eid=" + dayEvent.Name + "&uid=" + AccountID.ToString(), UriKind.Relative));
        }

        void CloseDayDetails(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DayDetails.Visibility = Visibility.Collapsed;
            RefreshAppointments();
        }

        /// <summary>
        /// Gibt das StackPanel zurück, in dem die Termine für einen Tag liegen
        /// </summary>
        /// <returns></returns>
        private StackPanel GetDayStackPanel(DateTime date)
        {
            if (date < _firstDayOfCalendarMonth || date > _lastDayOfCalendarMonth)
                return null;

            int sIndex = (int)(date.Date - _firstDayOfCalendarMonth).TotalDays;

            if (_dayPanels.ContainsKey(sIndex))
                return _dayPanels[sIndex];

            StackPanel newSPanel = new StackPanel();
            newSPanel.Orientation = Orientation.Vertical;
            Grid.SetColumn(newSPanel, sIndex % 7);
            Grid.SetRow(newSPanel, sIndex / 7 + 1);
            GrdAppointments.Children.Add(newSPanel);
            _dayPanels[sIndex] = newSPanel;

            return newSPanel;
        }

        private SolidColorBrush GetCalendarColor(int CalendarId)
        {
            var calendar = Context.Calendars.Where(o => o._accountId == AccountID && o.Id == CalendarId).FirstOrDefault();
            var converter = new OwnCloud.View.Converter.CalendarColorConverter();
            SolidColorBrush color = (SolidColorBrush)converter.Convert(calendar.CalendarColor, null, null, null);
            return color;
        }

        private IEnumerable<TableEvent> GetCalendarEvents(DateTime startTime, DateTime endTime)
        {
            var calendarEvents = Context.Calendars.Where(o => o._accountId == AccountID).Select(o => o.Events.Where(
                q => q.From >= startTime && q.To <= endTime
            ));
            IEnumerable<TableEvent> events = new TableEvent[0];

            foreach (var calendar in calendarEvents)
                events = events.Concat(calendar);

            events = events
                .OrderByDescending(o => o.To - o.From)
                .ToArray();

            return events;
        }

        private IEnumerable<TableEvent> GetCalendarEvents(List<int> eventId)
        {
            var calendarEvents = Context.Calendars.Where(o => o._accountId == AccountID).Select(o => o.Events.Where(q => eventId.Contains(q.EventId)));
            IEnumerable<TableEvent> events = new TableEvent[0];

            foreach (var calendar in calendarEvents)
                events = events.Concat(calendar);

            events = events
                .OrderByDescending(o => o.To - o.From)
                .ToArray();

            return events;
        }

        #endregion

        #region Private events

        private void GestureListener_OnDragCompleted(object sender, DragCompletedGestureEventArgs e)
        {
            if (e.Direction == Orientation.Vertical) return;

            SelectedDate = e.HorizontalChange > 0 ? SelectedDate.AddMonths(-1) : SelectedDate.AddMonths(1);
        }

        #endregion

        #region public events

        private void OnDateChanging()
        {
            if (DateChanging != null)
                DateChanging(this, new RoutedEventArgs());
        }
        public event RoutedEventHandler DateChanging;

        private void OnDateChanged()
        {
            if (DateChanged != null)
                DateChanged(this, new RoutedEventArgs());
        }
        public event RoutedEventHandler DateChanged;

        #endregion

    }
}