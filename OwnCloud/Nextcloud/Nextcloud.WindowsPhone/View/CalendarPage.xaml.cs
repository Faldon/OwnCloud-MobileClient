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

        public DateTime SelectedDate
        {
            get { return (DateTime)GetValue(SelectedDateProperty); }
            set
            {
                SetValue(SelectedDateProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for SelectedDate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register("SelectedDate", typeof(DateTime), typeof(CalendarPage), new PropertyMetadata(DateTime.MinValue, OnSelectedDateChanged));

        private static void OnSelectedDateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) {
            //(sender as CalendarPage).SelectedDateChanged(e);
        }

        private void SelectedDateChanged(DependencyPropertyChangedEventArgs e) {
            //OnDateChanging();
            //if ((DateTime)e.OldValue == DateTime.MinValue) {
            //    _firstDayOfCalendarMonth = ((DateTime)e.NewValue).FirstOfMonth().FirstDayOfWeek().Date;
            //    _lastDayOfCalendarMonth = ((DateTime)e.NewValue).LastOfMonth().LastDayOfWeek().AddDays(1);
            //}

            //if ((DateTime)e.NewValue > (DateTime)e.OldValue)
            //    this.SlideLeftBegin.Begin();
            //else this.SlideRightBegin.Begin();
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
            ChangeDate();
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
            //OnDateChanged();

            _firstDayOfCalendarMonth = SelectedDate.FirstOfMonth().FirstDayOfWeek().Date;
            _lastDayOfCalendarMonth = SelectedDate.LastOfMonth().LastDayOfWeek().AddDays(1);

            _weekCount = SelectedDate.GetMonthCount();
            ResetGridLines();

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
                    //SolidColorBrush indicatorBrush = new SolidColorBrush(Colors.White);
                    if (fieldDate.Date == DateTime.Now.Date) {
                        //var a = Application.Current.Resources.ThemeDictionaries.Keys;
                        dayIndicatorColor = PhoneAccentColor;
                       // indicatorBrush = (SolidColorBrush)GrdDayIndicator.Background;
                    } else if (fieldDate.Month != SelectedDate.Month) {
                        //indicatorBrush.Color = Colors.Gray;
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
    }
}
