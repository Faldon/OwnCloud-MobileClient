using System;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO;
using OwnCloud.Data;
using OwnCloud.Data.Calendar;
using OwnCloud.Data.Calendar.Parsing;
using OwnCloud.Extensions;
using OwnCloud.Net;

namespace OwnCloud.View.Page
{
    public partial class AppointmentPage : PhoneApplicationPage
    {
        public AppointmentPage()
        {
            InitializeComponent();
            ApplicationBar.TranslateButtons();

            this.Unloaded += AppointmentPage_Unloaded;
            TpTo.Value = TpTo.Value.Value.AddHours(1);
        }

        void AppointmentPage_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        private string _url;
        private int _accountId;

        private OwnCloudDataContext _context;
        public OwnCloudDataContext Context
        {
            get { return _context ?? (_context = new OwnCloudDataContext()); }
            set { _context = value; }
        }

        private Account _account;
        public Account Account
        {
            get { return _account ?? (Context.Accounts.Single(o => o.GUID == _accountId)); }
            set { _account = value; }
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                return;
            }

            //Load Account ID
            if (NavigationContext.QueryString.ContainsKey("uid"))
            { 
                _accountId = int.Parse(NavigationContext.QueryString["uid"]);                
            }

            CalendarPicker.ItemsSource = Context.Calendars;

            if (NavigationContext.QueryString.ContainsKey("eid"))
            {
                LoadFromEventID(Int32.Parse(NavigationContext.QueryString["eid"]));
            }

            if (NavigationContext.QueryString.ContainsKey("eTag"))
            {
                LoadFromETag(NavigationContext.QueryString["eTag"]);
            }

            base.OnNavigatedTo(e);
        }

        #region Load and Save and delete

        private void LoadFromETag(string eTag)
        {
            var dbEvent = Context.Events.SingleOrDefault(o => o.GetETag == eTag);
            var calendar = Context.Calendars.SingleOrDefault(c => c.Id == dbEvent.CalendarId);
            CalendarPicker.SelectedItem = calendar;

            if (dbEvent == null) return;

            var parser = new ParserICal();
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.Write(dbEvent.CalendarData);
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                var storedEvent = parser.Parse(stream).Events.First();

                TbTitle.Text = storedEvent.Title ?? "";
                DpFrom.Value = storedEvent.From;
                TpFrom.Value = storedEvent.From;
                DpTo.Value = storedEvent.To;
                TpTo.Value = storedEvent.To;
                TbDescription.Text = storedEvent.Description ?? "";
                TbLocation.Text = storedEvent.Location ?? "";
                CbFullDayEvent.IsChecked = storedEvent.IsFullDayEvent;
                _url = dbEvent.Url;

                if (!storedEvent.IsFullDayEvent)
                {
                    DpFrom.Value = storedEvent.From;
                    DpTo.Value = storedEvent.To;
                }
                else
                {
                    DpFrom.Value = storedEvent.From.Date;
                    DpTo.Value = storedEvent.To.Date.AddDays(-1);
                }

                TpFrom.Value = storedEvent.From;
                TpTo.Value = storedEvent.To;            
            }
        }

        private void LoadFromEventID(int eid)
        {
            var dbEvent = Context.Events.SingleOrDefault(o => o.EventId == eid);
            var calendar = Context.Calendars.SingleOrDefault(c => c.Id == dbEvent.CalendarId);
            CalendarPicker.SelectedItem = calendar;

            if (dbEvent == null) return;

            var parser = new ParserICal();
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.Write(dbEvent.CalendarData);
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                var storedEvent = parser.Parse(stream).Events.First();

                TbTitle.Text = storedEvent.Title ?? "";
                DpFrom.Value = storedEvent.From;
                TpFrom.Value = storedEvent.From;
                DpTo.Value = storedEvent.To;
                TpTo.Value = storedEvent.To;
                TbDescription.Text = storedEvent.Description ?? "";
                TbLocation.Text = storedEvent.Location ?? "";
                CbFullDayEvent.IsChecked = storedEvent.IsFullDayEvent;
                _url = dbEvent.Url;

                if (!storedEvent.IsFullDayEvent)
                {
                    DpFrom.Value = storedEvent.From;
                    DpTo.Value = storedEvent.To;
                }
                else
                {
                    DpFrom.Value = storedEvent.From.Date;
                    DpTo.Value = storedEvent.To.Date.AddDays(-1);
                }

                TpFrom.Value = storedEvent.From;
                TpTo.Value = storedEvent.To;
            }
        }

        private void SaveExisting()
        {
            LockPage();

            var dbEvent = Context.Events.SingleOrDefault(o => o.Url == _url);
            if (dbEvent == null) return;

            SaveTableEvent(dbEvent, true);
        }

        private void SaveNew()
        {
            LockPage();

            var dbEvent = new TableEvent()
            {
                Url = (CalendarPicker.SelectedItem as TableCalendar).Url + "owncloud_mobile-" + Utility.SHA1Hash(DateTime.Now.ToLongDateString()) + ".ics",
                CalendarId = (CalendarPicker.SelectedItem as TableCalendar).Id
            };
            if (dbEvent == null) return;

            SaveTableEvent(dbEvent, false);
        }

        private void SaveTableEvent(TableEvent dbEvent, bool merge)
        {
            dbEvent.From = (DpFrom.Value ?? DateTime.Now).CombineWithTime(TpFrom.Value ?? DateTime.Now);
            dbEvent.To = (DpTo.Value ?? DateTime.Now).CombineWithTime(TpTo.Value ?? DateTime.Now);
            dbEvent.Title = TbTitle.Text;
            dbEvent.IsFullDayEvent = CbFullDayEvent.IsChecked ?? false;

            if (dbEvent.IsFullDayEvent)
            {
                dbEvent.From = dbEvent.From.Date;
                dbEvent.To = dbEvent.To.Date.AddDays(1);
            }

            if (dbEvent.To < dbEvent.From)
            {
                MessageBox.Show(Resource.Localization.AppResources.AppointmentPage_WrongDate);
                UnlockPage();
                return;
            }

            if (!merge)
            {
                dbEvent.CalendarData = System.IO.File.OpenText("Assets/NewCalendar.ics").ReadToEnd();
                CalendarDataUpdater.UpdateCalendarData(dbEvent, TbDescription.Text, TbLocation.Text, true);
            }
            else
            {
                if (CalendarPicker.SelectedItem == Context.Calendars.SingleOrDefault(c => c.Id == dbEvent.CalendarId))
                {
                    CalendarDataUpdater.UpdateCalendarData(dbEvent, TbDescription.Text, TbLocation.Text, false);
                }
                else
                {
                    dbEvent.Url = (CalendarPicker.SelectedItem as TableCalendar).Url + dbEvent.Url.Substring(dbEvent.Url.LastIndexOf("/")).TrimStart('/');
                    CalendarDataUpdater.UpdateCalendarData(dbEvent, TbDescription.Text, TbLocation.Text, true);
                    DeleteExisting(false);
                }
            }
            
            var ocCLient = LoadOcCalendarClient();
            ocCLient.SaveEventComplete += ocCLient_SaveEventComplete;
            ocCLient.SaveEvent(dbEvent);

            Context.SubmitChanges();
        }

        private void ocCLient_SaveEventComplete(bool success)
        {
            //Todo: Error Handling
            Dispatcher.BeginInvoke(() =>
            {
                UnlockPage();
                App.Current.RootFrame.Navigate(new Uri("/View/Page/CalendarMonthPage.xaml?uid=" + _accountId.ToString(), UriKind.Relative));
            });
        }

        /// <summary>
        /// Deletes the current existing event
        /// </summary>
        private void DeleteExisting(bool redirect)
        {
            var client = LoadOcCalendarClient();
            if(redirect)
            {
                client.DeleteEventComplete += ocCLient_DeleteEventComplete;
            }
            client.DeleteEvent(_url);
        }

        void ocCLient_DeleteEventComplete(bool success)
        {
            //Todo: Error Handling
            Dispatcher.BeginInvoke(() =>
            {
                UnlockPage();
                App.Current.RootFrame.Navigate(new Uri("/View/Page/CalendarMonthPage.xaml?uid=" + _accountId.ToString(), UriKind.Relative));
            });
        }


        private OcCalendarClient LoadOcCalendarClient()
        {
            if (Account.IsEncrypted)
                Account.RestoreCredentials();

            return new OcCalendarClient(Account.GetUri().AbsoluteUri,
                                        new Net.OwncloudCredentials
                                            {
                                                Username = Account.Username,
                                                Password = Account.Password
                                            }, Account.CalDAVPath);
        }


        #endregion


        private void LockPage()
        {
            SystemTray.ProgressIndicator.IsVisible = true;
            this.ApplicationBar.IsVisible = false;
        }

        private void UnlockPage()
        {
            Dispatcher.BeginInvoke(() =>
                {
                    SystemTray.ProgressIndicator.IsVisible = false;
                    this.ApplicationBar.IsVisible = true;
                });
        }

        #region Page Events

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (_url != null)
            {
                SaveExisting();
            }
            else
            {
                SaveNew();
            }
                
        }

        private void OnDeleteClick(object sender, EventArgs e)
        {
            if (_url != null)
                DeleteExisting(true);
        }

        private void CbFullDayEventChanged(object sender, RoutedEventArgs e)
        {
            TpFrom.Visibility = TpTo.Visibility = CbFullDayEvent.IsChecked ?? false ? Visibility.Collapsed : Visibility.Visible;

        }
        #endregion
    }
}