using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using OwnCloud.Data;
using System.Windows.Navigation;
using OwnCloud.Resource.Localization;
using OwnCloud.View.Controls;

namespace OwnCloud.View.Page
{
    public partial class CalendarDayPage : PhoneApplicationPage
    {
        public CalendarDayPage()
        {
            InitializeComponent();
        }

        private int _userId;
        private CalendarDayOverview _dayOverview;

        public DateTime StartDate
        {
            get { return (DateTime)GetValue(StartDateProperty); }
            set
            {
                SetValue(StartDateProperty, value);
            }
        }


        public new CalendarDaysDataContext DataContext
        {
            get { return base.DataContext as CalendarDaysDataContext; }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //Get userid in query
            if (NavigationContext.QueryString.ContainsKey("uid"))
                _userId = int.Parse(NavigationContext.QueryString["uid"]);
            else throw new ArgumentNullException("uid", AppResources.Exception_NoUserID);

            try
            {
                StartDate = DateTime.Parse(NavigationContext.QueryString["startDate"]);
            }
            catch
            {
                StartDate = DateTime.Now;
            }

            base.DataContext = new CalendarDaysDataContext(StartDate);
            TbDateHeader.Text = StartDate.ToLongDateString();
            TbTodayHeader.Text = StartDate.ToShortDateString();
            TbTomorrowHeader.Text = StartDate.AddDays(1).ToShortDateString();

            base.OnNavigatedTo(e);
        }

        public static readonly DependencyProperty StartDateProperty =
            DependencyProperty.Register("StartDate", typeof(DateTime), typeof(CalendarDayPage), new PropertyMetadata(DateTime.MinValue, OnStartDateDateChanged));

        private static void OnStartDateDateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as CalendarDayPage).StartDateChanged(e);
        }

        private void StartDateChanged(DependencyPropertyChangedEventArgs e)
        {
            OnDateChanging();

            if ((DateTime)e.NewValue > (DateTime)e.OldValue)
                SlideLeftBegin.Begin();
            else SlideRightBegin.Begin();
        }

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

            var dataContext = base.DataContext as CalendarDaysDataContext;
            dataContext.Days = new System.Collections.ObjectModel.ObservableCollection<DateTime>();
            dataContext.Days.Add(StartDate);
            _dayOverview.AppointmentGrid.Children.Clear();

            TbDateHeader.Text = StartDate.ToLongDateString();
            TbTodayHeader.Text = StartDate.ToShortDateString();
            TbTomorrowHeader.Text = StartDate.AddDays(1).ToShortDateString();

            Dispatcher.BeginInvoke(() => { _dayOverview.UpdateEvents(); });
        }

        private void GestureListener_OnDragCompleted(object sender, DragCompletedGestureEventArgs e)
        {
            if (e.Direction == System.Windows.Controls.Orientation.Vertical) return;

            StartDate = e.HorizontalChange > 0 ? StartDate.AddDays(-1) : StartDate.AddDays(1);
        }

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

        private void LongListSelector_OnLink(object sender, ItemRealizationEventArgs e)
        {
            DataContext.ItemLinked(sender, e);
        }

        private void LayoutRoot_OnLoaded(object sender, RoutedEventArgs e)
        {
            //LlsDays.ScrollTo(_startDate);
        }

        private void DayList_Loaded(object sender, RoutedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            _dayOverview = FindChildOfType<OwnCloud.View.Controls.CalendarDayOverview>(element);
        }

        void _dayScoller_LayoutUpdated(object sender, EventArgs e)
        {
            //if (_dayScoller.VerticalOffset < 1)
            //{
            //    DataContext.AddOnTop();
            //    LlsDays.ScrollTo(DataContext.Days[1]);
            //    _dayScoller.ScrollToVerticalOffset(2);
            //}
        }

        public static T FindChildOfType<T>(DependencyObject root) where T : class
        {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                for (int i = VisualTreeHelper.GetChildrenCount(current) - 1; 0 <= i; i--)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    var typedChild = child as T;
                    if (typedChild != null)
                    {
                        return typedChild;
                    }
                    queue.Enqueue(child);
                }
            }
            return null;
        }

        private void DynamicCalendarSource_OnOnEventsRequested(object sender, DynamicCalendarSource.LoadEventResult e)
        {
            var validCalendars = App.DataContext.Calendars.Where(o => o._accountId == _userId).Select(o => o.Id).ToArray();
            e.Result = App.DataContext.Events.Where(o => validCalendars.Contains(o.CalendarId));
        }
    }
}