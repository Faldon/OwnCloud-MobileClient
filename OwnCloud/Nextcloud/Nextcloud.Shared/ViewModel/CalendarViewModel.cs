using Nextcloud.Data;
using Nextcloud.DAV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Linq;
using SQLiteNetExtensions.Extensions;
using Windows.UI.Core;

namespace Nextcloud.ViewModel
{
    class CalendarViewModel: ViewModel
    {
        private CoreDispatcher dispatcher;
        private ObservableCollection<Calendar> _calendarCollection;
        public ObservableCollection<Calendar> CalendarCollection
        {
            get
            {
                return _calendarCollection;
            }
            set
            {
                _calendarCollection = value;
                NotifyPropertyChanged();
            }
        }

        private ObservableCollection<CalendarEvent> _eventCollcetion;
        public ObservableCollection<CalendarEvent> EventCollection
        {
            get
            {
                return _eventCollcetion;
            }
            set
            {
                _eventCollcetion = value;
                NotifyPropertyChanged();
            }
        }


        private bool _isFetching;
        public bool IsFetching
        {
            get
            {
                return _isFetching;
            }
            set
            {
                _isFetching = value;
                NotifyPropertyChanged();
            }
        }

        private string _lastError;
        public string LastError
        {
            get
            {
                return _lastError;
            }
            set
            {
                _lastError = value;
                NotifyPropertyChanged();
            }
        }

        public CalendarViewModel(ObservableCollection<Calendar> calendars) {
            CalendarCollection = calendars;
            foreach (Calendar cal in CalendarCollection) {
                App.GetDataContext().GetConnection().GetChildren(cal, true);
            }
            //EventCollection = new ObservableCollection<CalendarEvent>(calendars.SelectMany(c => c.CalendarEvents).ToList());
            IsFetching = false;
            FetchAppointmentsAsync();
        }

        public async void FetchAppointmentsAsync() {
            IsFetching = true;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            List<Account> accountList = CalendarCollection.Select(c => c.Account).Distinct().ToList();
            foreach(Account account in accountList) {
                App.GetDataContext().GetConnection().GetChildren(account);
                NetworkCredential cred = await account.GetCredential();
                var webdav = new WebDAV(new Uri(account.Server.Protocol + "://" + account.Server.FQDN, UriKind.Absolute), cred);
                foreach (Calendar calendar in CalendarCollection.Where(c => c.Account == account).ToList()) {
                    webdav.StartRequest(DAVRequestHeader.CreateReport(calendar.Path), DAVRequestBody.CreateCondensedCalendarEventRequest(), calendar, OnFetchAppointmentsAsyncComplete);
                }
            }
        }

        private async void OnFetchAppointmentsAsyncComplete(DAVRequestResult result, object userObj) {
            if (result.Status == ServerStatus.MultiStatus && !result.Request.ErrorOccured && result.Items.Count > 0) {
                Calendar _calendar = userObj as Calendar;
                foreach (DAVRequestResult.Item item in result.Items) {
                    var eventInDatabase = EventCollection.Where(e => e.CalendarId == _calendar.CalendarId && e.Path == item.Reference).FirstOrDefault();
                    if(eventInDatabase != null && eventInDatabase.ETag == item.ETag) {
                        continue;
                    }
                    CalendarEvent eventItem = new CalendarEvent() {
                        CalendarEventId = eventInDatabase != null ? eventInDatabase.CalendarEventId : null,
                        Path = item.Reference,
                        ETag = item.ETag,
                        CalendarId = _calendar.AccountId,
                        Calendar = _calendar
                    };
                    App.GetDataContext().StoreCalendarEventAsync(eventItem);

                        //await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CalendarCollection.Add(calendarItem));
                }
                //await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { SyncDatabaseAsync(); });
            } else {
                //foreach (Calendar calendarFromDatabase in App.GetDataContext().GetConnection().Table<Calendar>().ToList()) {
                //    //dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CalendarCollection.Add(calendarFromDatabase));
                //}
            }
        }
    }
}
