using Nextcloud.Common;
using Nextcloud.ViewModel;
using System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Nextcloud.Data;
using Windows.UI.Popups;
using Nextcloud.DAV;
using Windows.UI.Core;
using System.Collections.Generic;

namespace Nextcloud.View
{
    public sealed partial class AppointmentDetailPage : Page
    {
        private int _eventID;
        private int _accountId;
        //private CalendarColorConverter _calendarColorConverter;
        //private ToUppercaseConverter _toUpperCaseConverter;
        //private SolidColorBrush _calendarColor;

        //private OwnCloudDataContext _context;
        //public OwnCloudDataContext Context
        //{
        //    get { return _context ?? (_context = new OwnCloudDataContext()); }
        //    set { _context = value; }
        //}

        public AppointmentDetailPage()
        {
            InitializeComponent();
            //ApplicationBar.TranslateButtons();
            //_calendarColorConverter = new OwnCloud.View.Converter.CalendarColorConverter();
        }

        //protected override void OnNavigatedTo(NavigationEventArgs e)
        //{
        //    if (e.NavigationMode == NavigationMode.Back)
        //    {
        //        return;
        //    }

        //    //Load Account ID
        //    if (NavigationContext.QueryString.ContainsKey("uid"))
        //    {
        //        _accountId = int.Parse(NavigationContext.QueryString["uid"]);
        //    }

        //    if (NavigationContext.QueryString.ContainsKey("eid"))
        //    {
        //        _eventID = int.Parse(NavigationContext.QueryString["eid"]);
        //        LoadFromEventID(Int32.Parse(NavigationContext.QueryString["eid"]));
        //    }

        //    base.OnNavigatedTo(e);
        //}

        //private void LoadFromEventID(int eid)
        //{
        //    var dbEvent = Context.Events.SingleOrDefault(o => o.EventId == eid);
        //    var calendar = Context.Calendars.SingleOrDefault(c => c.Id == dbEvent.CalendarId);
        //    _calendarColor = (SolidColorBrush)_calendarColorConverter.Convert(calendar.CalendarColor, null, null, null);

        //    if (dbEvent == null) return;

        //    var parser = new ParserICal();
        //    using (var stream = new MemoryStream())
        //    {
        //        var writer = new StreamWriter(stream);
        //        writer.Write(dbEvent.CalendarData);
        //        writer.Flush();
        //        stream.Seek(0, SeekOrigin.Begin);

        //        var storedEvent = parser.Parse(stream).Events.First();

        //        EventTitle.Text = storedEvent.Title ?? "";
        //        EventTitle.Foreground = _calendarColor;
        //        EventDate.Text = storedEvent.To.Subtract(storedEvent.From) <= new TimeSpan(1, 0, 0, 0) ? storedEvent.From.Date.ToLongDateString() : storedEvent.From.ToShortDateString() + " - " + storedEvent.To.ToShortDateString();
        //        EventDate.Foreground = _calendarColor;
        //        EventDescription.Text = storedEvent.Description ?? "";
        //        EventLocation.Text = storedEvent.Location ?? "";
        //        EventLocation.Foreground = _calendarColor;
        //        EventLocation.Visibility = EventLocation.Text == "" ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        //        EventCalendar.Text = calendar.DisplayName;
        //        if (!storedEvent.IsFullDayEvent)
        //        {
        //            EventTime.Text = storedEvent.From.ToShortTimeString() + " - " + storedEvent.To.ToShortTimeString();
        //            EventTime.Foreground = _calendarColor;
        //            EventTime.Visibility = System.Windows.Visibility.Visible;
        //        }
        //    }
        //}

        //private void EditAppointment(object sender, EventArgs e)
        //{
        //    App.Current.RootFrame.Navigate(new Uri("/View/Page/AppointmentPage.xaml?eid=" + _eventID.ToString() + "&uid=" + _accountId.ToString(), UriKind.Relative));
        //}

        private void AppBarButton_Click(object sender, RoutedEventArgs e) {

        }
    }
}