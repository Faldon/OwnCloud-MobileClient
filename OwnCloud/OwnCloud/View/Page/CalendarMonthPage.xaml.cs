using System;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using OwnCloud.Data;
using OwnCloud.Net;
using OwnCloud.Resource.Localization;
using System.Linq;
using OwnCloud.Extensions;

namespace OwnCloud.View.Page
{
    public partial class CalendarMonthPage : PhoneApplicationPage
    {


        public CalendarMonthPage()
        {
            InitializeComponent();
            ApplicationBar.TranslateButtons();
            
            ApplicationBarMenuItem dayView = new ApplicationBarMenuItem(Resource.Localization.AppResources.ApplicationBarMenuItem_Day);
            dayView.Click += GotoDayView;
            ApplicationBarMenuItem monthView = new ApplicationBarMenuItem(Resource.Localization.AppResources.ApplicationBarMenuItem_Month);
            monthView.IsEnabled = false;
            ApplicationBar.MenuItems.Add(dayView);
            ApplicationBar.MenuItems.Add(monthView);

            this.Unloaded += CalendarMonthPage_Unloaded;
        }

        void CalendarMonthPage_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        #region private fields

        private int _userId = 0;

        private OwnCloudDataContext _context;
        public OwnCloudDataContext Context
        {
            get { return _context ?? (_context = new OwnCloudDataContext()); }
            set { _context = value; }
        }

        private Account _account;
        public Account Account
        {
            get
            {
                if (_account == null)
                {
                    _account = Context.Accounts.Single(o => o.GUID == _userId);
                    if (_account.IsEncrypted)
                        _account.RestoreCredentials();
                }
                return _account;
            }
            set { _account = value; }
        }

        private DateTime? _selectedDate;
        public DateTime SelectedDate
        {
            get { return (DateTime)(_selectedDate.HasValue ? _selectedDate.Value : (_selectedDate = DateTime.Now)); }
            set { _selectedDate = value; }
        }


        #endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //Get userid in query
            if (NavigationContext.QueryString.ContainsKey("uid"))
                _userId = int.Parse(NavigationContext.QueryString["uid"]);
            else throw new ArgumentNullException("uid", AppResources.Exception_NoUserID);

            CcCalendar.AccountID = _userId;
            CcCalendar.SelectedDate = SelectedDate;

            ReloadAppointments();

            base.OnNavigatedTo(e);
        }

        private void ReloadAppointments()
        {
            LockPage();
            SetLoading();
            var sync = new CalendarSync();
            sync.SyncComplete += sync_SyncComplete;
            sync.Sync(Account.GetUri().AbsoluteUri, new Net.OwncloudCredentials { Username = Account.Username, Password = Account.Password }, Account.CalDAVPath);
        }

        void sync_SyncComplete(bool success)
        {
            if (!success)
            {
                Dispatcher.BeginInvoke(() => { MessageBox.Show(Resource.Localization.AppResources.Sync_Error); });
            }
            Dispatcher.BeginInvoke(() => { CcCalendar.RefreshAppointments(); UnlockPage(); UnsetLoading(); });
        }


        #region Private events

        private void GotoCalendarSettings(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Page/CalendarSelectPage.xaml?uid=" + _userId.ToString(), UriKind.Relative));
        }

        private void GotoDayView(object sender, EventArgs e)
        {
            App.Current.RootFrame.Navigate(new Uri("/View/Page/CalendarDayPage.xaml?uid=" + CcCalendar.AccountID.ToString() + "&startDate=" + CcCalendar.SelectedDate.ToShortDateString(),
                                                   UriKind.Relative));
        }

        private void GotoAppointmentView(object sender, EventArgs e)
        {
            App.Current.RootFrame.Navigate(new Uri("/View/Page/AppointmentPage.xaml?uid=" + CcCalendar.AccountID.ToString(), UriKind.Relative));
        }

        private void CcCalendar_OnDateChanged(object sender, RoutedEventArgs e)
        {
            TbYearHeader.Text = CcCalendar.SelectedDate.ToString("yyyy");
            TbMonthHeader.Text = CcCalendar.SelectedDate.ToString("MMMM");
            TbNextMonthHeader.Text = CcCalendar.SelectedDate.AddMonths(1).ToString("MMMM");
            SelectedDate = CcCalendar.SelectedDate.Date;
        }

        private void ReloadCalendarEvents(object sender, EventArgs e)
        {
            ReloadAppointments();
        }

        #endregion

        private void LockPage()
        {
            foreach (var button in ApplicationBar.Buttons.OfType<ApplicationBarIconButton>())
            {
                button.IsEnabled = false;
            }
            IsEnabled = false;
        }
        private void UnlockPage()
        {
            foreach (var button in ApplicationBar.Buttons.OfType<ApplicationBarIconButton>())
            {
                button.IsEnabled = true;
            }
            IsEnabled = true;
        }
        private void SetLoading()
        {
            if (SystemTray.ProgressIndicator != null)
                SystemTray.ProgressIndicator.IsVisible = true;
        }
        private void UnsetLoading()
        {
            if (SystemTray.ProgressIndicator != null)
                SystemTray.ProgressIndicator.IsVisible = false;
        }

    }
}