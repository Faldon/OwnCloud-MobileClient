using Nextcloud.Common;
using Nextcloud.Data;
using Nextcloud.ViewModel;
using System;
using System.Collections.ObjectModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Nextcloud.Extensions;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using System.Collections.Specialized;
using Nextcloud.Shared.Converter;
using SQLiteNetExtensions.Extensions;
using System.ComponentModel;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Nextcloud.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CalendarPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private int _weekCount = 0;
        private DateTime _firstDayOfCalendarMonth;
        private DateTime _lastDayOfCalendarMonth;
        private Dictionary<int, StackPanel> _dayPanels = new Dictionary<int, StackPanel>();
        private Color PhoneAccentColor;
        private Point initialPoint;

        public DateTime SelectedDate
        {
            get { return (DateTime)GetValue(SelectedDateProperty); }
            set
            {
                SetValue(SelectedDateProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for SelectedDate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register("SelectedDate", typeof(DateTime), typeof(CalendarPage), new PropertyMetadata(DateTime.MinValue, OnSelectedDateChange));

        private static void OnSelectedDateChange(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            (sender as CalendarPage).SelectedDateChanged(e);
        }

        private void SelectedDateChanged(DependencyPropertyChangedEventArgs e) {
            CurrentYear.Text = SelectedDate.Year.ToString();
            CurrentMonth.Text = SelectedDate.ToString("MMMM");
            NextMonth.Text = SelectedDate.AddMonths(1).ToString("MMMM");
            OnDateChanging();
            if ((DateTime)e.OldValue == DateTime.MinValue) {
                _firstDayOfCalendarMonth = ((DateTime)e.NewValue).FirstOfMonth().FirstDayOfWeek().Date;
                _lastDayOfCalendarMonth = ((DateTime)e.NewValue).LastOfMonth().LastDayOfWeek().AddDays(1);
            }

            if ((DateTime)e.NewValue > (DateTime)e.OldValue) {
                this.SlideLeftBegin.Begin();
            } else {
                this.SlideRightBegin.Begin();
            }
        }

        public CalendarPage() {
            this.InitializeComponent();
            PhoneAccentColor = (GrdDayIndicator.Background as SolidColorBrush).Color;
            GrdDayIndicator.Background = null;
            SelectedDate = DateTime.Now;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e) {
            ObservableCollection<Calendar> dataModel = e.NavigationParameter as ObservableCollection<Calendar>;
            if (dataModel == null) {
                LayoutRoot.DataContext = new CalendarViewModel(new ObservableCollection<Calendar>());
            } else {
                LayoutRoot.DataContext = new CalendarViewModel(dataModel);
            }
            (LayoutRoot.DataContext as CalendarViewModel).PropertyChanged += OnPropertyChanged;
            //ChangeDate();
        }

        private async void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "EventCollection") {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => ChangeDate());
            }
        }

        private void PutEvents() {
            //if(e.PropertyName == "EventCollection") {
                //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => GrdAppointments.Children.Clear());
                //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => _dayPanels.Clear());
                var events = (LayoutRoot.DataContext as CalendarViewModel).EventCollection.Where(q => (q.EndDate >= _firstDayOfCalendarMonth && q.StartDate <= _lastDayOfCalendarMonth) || (q.IsRecurringEvent && q.StartDate <= _lastDayOfCalendarMonth)).ToList();
                foreach (CalendarEvent calendarEvent in events) {
                    App.GetDataContext().GetConnection().GetChildren(calendarEvent, recursive: true);
                    App.GetDataContext().GetConnection().GetChildren(calendarEvent.CalendarObject, recursive: true);
                var currentDate = calendarEvent.StartDate.ToLocalTime().Date;
                    var endDate = calendarEvent.EndDate.ToLocalTime().Date;

                    if (calendarEvent.IsRecurringEvent) {
                        //foreach(RecurrenceRule rrule in calendarEvent.RecurrenceRules) {
                        //    string rFrequency = (string)rRules.Single(r => r.Key == "FREQ").Value;
                        //    int rInterval = (int)rRules.Single(r => r.Key == "INTERVAL").Value;
                        //    DateTime rEnd = ((string)rRules.SingleOrDefault(r => r.Key == "UNTIL").Key == "UNTIL") ? Convert.ToDateTime(rRules.Single(r => r.Key == "UNTIL").Value) : DateTime.MaxValue;
                        //    int rCount = (string)rRules.SingleOrDefault(r => r.Key == "COUNT").Key == "COUNT" ? (int)rRules.Single(r => r.Key == "COUNT").Value : 0;

                        //    if((int)rrule.Count > 0) {

                        //    }

                        //    if (rCount == 0 && rEnd < _firstDayOfCalendarMonth) { continue; } else if (rCount > 0) {
                        //        for (var i = 0; i < rCount; i++) {
                        //            switch (rFrequency) {
                        //                case "DAILY":
                        //                    currentDate = calendarEvent.StartDate.Date.AddDays(i * rInterval);
                        //                    endDate = calendarEvent.EndDate.Date.AddDays(i * rInterval);
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                    break;
                        //                case "WEEKLY":
                        //                    currentDate = calendarEvent.StartDate.Date.AddDays(i * rInterval * 7);
                        //                    endDate = calendarEvent.EndDate.Date.AddDays(i * rInterval * 7);
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                    break;
                        //                case "MONTHLY":
                        //                    currentDate = calendarEvent.StartDate.Date.AddMonths(i * rInterval);
                        //                    endDate = calendarEvent.EndDate.Date.AddMonths(i * rInterval);
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                    break;
                        //                case "YEARLY":
                        //                    currentDate = calendarEvent.StartDate.Date.AddYears(i * rInterval);
                        //                    endDate = calendarEvent.EndDate.Date.AddYears(i * rInterval);
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                    break;
                        //            }
                        //        }
                        //    } else if (rEnd > _firstDayOfCalendarMonth) {
                        //        switch (rFrequency) {
                        //            case "DAILY":
                        //                currentDate = currentDate.AddDays((_firstDayOfCalendarMonth - currentDate).TotalDays + (_firstDayOfCalendarMonth - currentDate).TotalDays % rInterval);
                        //                endDate = endDate.AddDays((_firstDayOfCalendarMonth - endDate).TotalDays + (_firstDayOfCalendarMonth - endDate).TotalDays % rInterval);
                        //                while (currentDate < calendarEvent.StartDate) {
                        //                    currentDate = currentDate.AddDays(rInterval);
                        //                    endDate = endDate.AddDays(rInterval);
                        //                }
                        //                while (currentDate <= rEnd && currentDate <= _lastDayOfCalendarMonth) {
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                    currentDate = currentDate.AddDays(rInterval);
                        //                    endDate = endDate.AddDays(rInterval);
                        //                }
                        //                break;
                        //            case "WEEKLY":
                        //                currentDate = calendarEvent.StartDate;
                        //                endDate = calendarEvent.EndDate;
                        //                while (currentDate <= _firstDayOfCalendarMonth) {
                        //                    currentDate = currentDate.AddDays(7 * rInterval);
                        //                    endDate = endDate.AddDays(7 * rInterval);
                        //                }
                        //                while (currentDate <= rEnd && currentDate <= _lastDayOfCalendarMonth) {
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                    currentDate = currentDate.AddDays(7 * rInterval);
                        //                    endDate = endDate.AddDays(7 * rInterval);
                        //                }
                        //                break;
                        //            case "MONTHLY":
                        //                currentDate = calendarEvent.StartDate;
                        //                endDate = calendarEvent.EndDate;
                        //                while (currentDate.Date < SelectedDate.Date) {
                        //                    currentDate = currentDate.AddMonths(rInterval);
                        //                    endDate = endDate.AddMonths(rInterval);
                        //                }
                        //                if (currentDate <= rEnd && currentDate >= _firstDayOfCalendarMonth && currentDate <= _lastDayOfCalendarMonth) {
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                }
                        //                break;
                        //            case "YEARLY":
                        //                currentDate = calendarEvent.StartDate;
                        //                endDate = calendarEvent.EndDate;
                        //                while (currentDate.Year < SelectedDate.Year) {
                        //                    currentDate = currentDate.AddYears(rInterval);
                        //                    endDate = endDate.AddYears(rInterval);
                        //                }
                        //                if (currentDate <= rEnd && currentDate >= _firstDayOfCalendarMonth && currentDate <= _lastDayOfCalendarMonth) {
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                }
                        //                break;
                        //        }
                        //    } else if (rCount == 0 && rEnd == DateTime.MaxValue) {
                        //        switch (rFrequency) {
                        //            case "DAILY":
                        //                currentDate = currentDate.AddDays((_firstDayOfCalendarMonth - currentDate).TotalDays + (_firstDayOfCalendarMonth - currentDate).TotalDays % rInterval);
                        //                endDate = endDate.AddDays((_firstDayOfCalendarMonth - endDate).TotalDays + (_firstDayOfCalendarMonth - endDate).TotalDays % rInterval);
                        //                while (currentDate < calendarEvent.StartDate) {
                        //                    currentDate = currentDate.AddDays(rInterval);
                        //                    endDate = endDate.AddDays(rInterval);
                        //                }
                        //                while (currentDate <= _lastDayOfCalendarMonth) {
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                    currentDate = currentDate.AddDays(rInterval);
                        //                    endDate = endDate.AddDays(rInterval);
                        //                }
                        //                break;
                        //            case "WEEKLY":
                        //                currentDate = calendarEvent.StartDate;
                        //                endDate = calendarEvent.EndDate;
                        //                while (currentDate < _firstDayOfCalendarMonth) {
                        //                    currentDate = currentDate.AddDays(7 * rInterval);
                        //                    endDate = endDate.AddDays(7 * rInterval);
                        //                }
                        //                while (currentDate <= _lastDayOfCalendarMonth) {
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                    currentDate = currentDate.AddDays(7 * rInterval);
                        //                    endDate = endDate.AddDays(7 * rInterval);
                        //                }
                        //                break;
                        //            case "MONTHLY":
                        //                while (currentDate.Date < SelectedDate.Date) {
                        //                    currentDate = currentDate.AddMonths(rInterval);
                        //                    endDate = endDate.AddMonths(rInterval);
                        //                }
                        //                if (currentDate >= _firstDayOfCalendarMonth && currentDate <= _lastDayOfCalendarMonth) {
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                }
                        //                break;
                        //            case "YEARLY":
                        //                currentDate = calendarEvent.StartDate;
                        //                endDate = calendarEvent.EndDate;
                        //                while (currentDate.Year < SelectedDate.Year) {
                        //                    currentDate = currentDate.AddYears(rInterval);
                        //                    endDate = endDate.AddYears(rInterval);
                        //                }
                        //                if (currentDate >= _firstDayOfCalendarMonth && currentDate <= _lastDayOfCalendarMonth) {
                        //                    PutSingleEvent(calendarEvent, currentDate, endDate);
                        //                }
                        //                break;
                        //        }
                        //    }
                        //}
                    } else {
                        PutSingleEvent(calendarEvent, currentDate, endDate);
                    }
                }
            //}

        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e) {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        public void ChangeDate() {
            OnDateChanged();

            _firstDayOfCalendarMonth = SelectedDate.FirstOfMonth().FirstDayOfWeek().Date;
            _lastDayOfCalendarMonth = SelectedDate.LastOfMonth().LastDayOfWeek().AddDays(1);
            _weekCount = SelectedDate.GetMonthCount();

            ResetGridLines();

            //Delete displayed events
            GrdAppointments.Children.Clear();
            _dayPanels.Clear();

            //Insert new events
            PutEvents();

            //Dispatcher.BeginInvoke(new Action(() => RefreshAppointments()));
        }

        private void ResetGridLines() {
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
            for (int i = 0; i < 7; i++) {
                for (int j = 0; j < _weekCount; j++) {
                    DateTime fieldDate = firstDay.AddDays((j * 7) + i);

                    Color dayIndicatorColor = Colors.White;
                    if (fieldDate.Date == DateTime.Now.Date) {
                        dayIndicatorColor = PhoneAccentColor;
                    } else if (fieldDate.Month != SelectedDate.Month) {
                        dayIndicatorColor = Colors.Gray;
                    }

                    var dayIndicator = new TextBlock {
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Text = fieldDate.Day.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Foreground = new SolidColorBrush(dayIndicatorColor),
                        Margin = new Thickness(5, 0, 0, 5)
                    };
                    Grid.SetColumn(dayIndicator, i);
                    Grid.SetRow(dayIndicator, j + 1);
                    GrdDayIndicator.Children.Add(dayIndicator);

                    var dayOpenControl = new StackPanel {
                        Background = new SolidColorBrush(Colors.Black),
                        Name = fieldDate.ToString()
                    };
                    //dayOpenControl.Tap += ShowDayDetails;
                    Grid.SetColumn(dayOpenControl, i);
                    Grid.SetRow(dayOpenControl, j + 1);
                    GrdCalendarLines.Children.Add(dayOpenControl);
                }
            }

            for (int i = 0; i < 6; i++) {
                var vRect = new Rectangle();
                vRect.Fill = new SolidColorBrush(Colors.White);
                vRect.Width = 2;
                vRect.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetRow(vRect, 1);
                Grid.SetRowSpan(vRect, _weekCount);
                Grid.SetColumn(vRect, i);

                GrdCalendarLines.Children.Add(vRect);
            }
            for (int i = 0; i < _weekCount + 1; i++) {
                var hRect = new Rectangle();
                hRect.Fill = new SolidColorBrush(Colors.White);
                hRect.Height = 2;
                hRect.VerticalAlignment = VerticalAlignment.Bottom;
                Grid.SetColumnSpan(hRect, 7);
                Grid.SetRow(hRect, i);

                GrdCalendarLines.Children.Add(hRect);
            }

            var targetDate = _firstDayOfCalendarMonth;
            for (int i = 0; i < 7; i++) {

                TextBlock dayBlock = new TextBlock();
                Grid.SetColumn(dayBlock, i);
                dayBlock.VerticalAlignment = VerticalAlignment.Bottom;
                dayBlock.HorizontalAlignment = HorizontalAlignment.Center;
                dayBlock.Text = targetDate.ToString("ddd");
                GrdCalendarLines.Children.Add(dayBlock);

                targetDate = targetDate.AddDays(1);
            }
        }

        private void PutSingleEvent(CalendarEvent calendarEvent, DateTime currentDate, DateTime endDate) {
            if (endDate == currentDate)
                endDate = endDate.AddSeconds(1);

            while (currentDate < endDate) {
                StackPanel dPanel = GetDayStackPanel(currentDate);

                if (dPanel == null) { currentDate = currentDate.AddDays(1); continue; }
                var converter = new HexcodeColorConverter();
                SolidColorBrush color = (SolidColorBrush)converter.Convert(calendarEvent.CalendarObject.Calendar.Color, null, null, null);

                var rect = new Rectangle {
                    Name = calendarEvent.CalendarObjectId.ToString() + "_" + calendarEvent.CalendarEventId.ToString() + "_" + currentDate.ToString(),
                    Fill = color,
                    Width = GrdAppointments.ColumnDefinitions[0].ActualWidth * 0.35,
                    Height = 2.5,
                    RadiusX = 1.5,
                    RadiusY = 1.5,
                    Margin = new Thickness(0, 3, 5, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Visibility = Visibility.Visible
                };
                dPanel.Children.Add(rect);
                currentDate = currentDate.AddDays(1);
                GrdAppointments.UpdateLayout();
            }

        }

        private StackPanel GetDayStackPanel(DateTime date) {
            if (date < _firstDayOfCalendarMonth || date > _lastDayOfCalendarMonth) {
                return null;
            }
            int sIndex = (int)(date.Date - _firstDayOfCalendarMonth).TotalDays;

            if (_dayPanels.ContainsKey(sIndex)) {
                return _dayPanels[sIndex];
            }
            StackPanel newSPanel = new StackPanel();
            newSPanel.Orientation = Orientation.Vertical;
            Grid.SetColumn(newSPanel, sIndex % 7);
            Grid.SetRow(newSPanel, sIndex / 7 + 1);
            GrdAppointments.Children.Add(newSPanel);
            _dayPanels[sIndex] = newSPanel;

            return newSPanel;
        }

        private void SlideLeftBegin_OnCompleted(object sender, object e) {
            ChangeDate();
            SlideLeftEnd.Begin();
        }

        private void SlideRightBegin_OnCompleted(object sender, object e) {
            ChangeDate();
            SlideRightEnd.Begin();
        }

        #region Private events

        private void OnFlick() {

        }

        #endregion

        #region public events

        private void OnDateChanging() {
            if (DateChanging != null)
                DateChanging(this, new RoutedEventArgs());
        }
        public event RoutedEventHandler DateChanging;

        private void OnDateChanged() {
            if (DateChanged != null)
                DateChanged(this, new RoutedEventArgs());
        }
        public event RoutedEventHandler DateChanged;

        #endregion

        private void ContentRoot_ManipulationStarted(object sender, Windows.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e) {
            initialPoint = e.Position;
        }

        private void ContentRoot_ManipulationDelta(object sender, Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e) {
            if(e.IsInertial) {
                if(e.Position.X - initialPoint.X >= 300) {
                    SelectedDate = SelectedDate.AddMonths(-1);
                    e.Complete();
                }
                else if (initialPoint.X - e.Position.X >= 300) {
                    SelectedDate = SelectedDate.AddMonths(1);
                    e.Complete();
                }
            }
        }
    }
}
