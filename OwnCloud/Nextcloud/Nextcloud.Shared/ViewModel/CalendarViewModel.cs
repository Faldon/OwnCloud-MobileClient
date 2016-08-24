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
            EventCollection = new ObservableCollection<CalendarEvent>(calendars.SelectMany(c => c.CalendarObjects).SelectMany(o => o.CalendarEvents).ToList());
            IsFetching = false;
            FetchCalendarObjectsAsync();
        }

        public async void FetchCalendarObjectsAsync() {
            IsFetching = true;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            List<Account> accountList = CalendarCollection.Select(c => c.Account).Distinct().ToList();
            foreach(Account account in accountList) {
                App.GetDataContext().GetConnection().GetChildren(account);
                NetworkCredential cred = await account.GetCredential();
                var webdav = new WebDAV(new Uri(account.Server.Protocol + "://" + account.Server.FQDN, UriKind.Absolute), cred);
                foreach (Calendar calendar in CalendarCollection.Where(c => c.AccountId == account.AccountId).ToList()) {
                    webdav.StartRequest(DAVRequestHeader.CreateReport(calendar.Path), DAVRequestBody.CreateCondensedCalendarRequest(), calendar, OnFetchCalendarObjectsAsyncComplete);
                }
            }
        }

        private async void OnFetchCalendarObjectsAsyncComplete(DAVRequestResult result, object userObj) {
            if (result.Status == ServerStatus.MultiStatus && !result.Request.ErrorOccured) {
                Calendar _calendar = userObj as Calendar;
                foreach (DAVRequestResult.Item item in result.Items) {
                    var calObjInDB = _calendar.CalendarObjects.Where(e => e.Path == item.Reference).FirstOrDefault();
                    if (calObjInDB != null && calObjInDB.ETag == item.ETag) {
                        continue;
                    }
                    CalendarObject calObj = new CalendarObject() {
                        CalendarObjectId = calObjInDB != null ? calObjInDB.CalendarObjectId : null,
                        Path = item.Reference,
                        ETag = item.ETag,
                        InSync = false,
                        CalendarId = _calendar.CalendarId,
                        Calendar = _calendar
                    };
                    App.GetDataContext().StoreCalendarObjectAsync(calObj);
                }
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { SyncDatabaseAsync(_calendar); });
            } else {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => IsFetching = false);
            }
        }

        private async void SyncDatabaseAsync(Calendar calendar) {
            List<CalendarObject> unsyncedCalObjs = await App.GetDataContext().GetUnsyncedCalendarObjectsAsync();
            unsyncedCalObjs = unsyncedCalObjs.Where(e => e.CalendarId == calendar.CalendarId).ToList();

            Account account = calendar.Account;
            App.GetDataContext().GetConnection().GetChildren(account, recursive: true);

            NetworkCredential cred = await account.GetCredential();
            var webdav = new WebDAV(new Uri(account.Server.Protocol + "://" + account.Server.FQDN, UriKind.Absolute), cred);
            webdav.StartRequest(DAVRequestHeader.CreateReport(calendar.Path), DAVRequestBody.CreateCalendarMultiget(unsyncedCalObjs), calendar, OnSyncDatabaseAsyncComplete);
        }

        private async void OnSyncDatabaseAsyncComplete(DAVRequestResult result, object userObj) {
            if (result.Status == ServerStatus.MultiStatus && !result.Request.ErrorOccured) {
                Calendar _calendar = userObj as Calendar;
                foreach (DAVRequestResult.Item item in result.Items) {
                    CalendarObject fromDatabase = await App.GetDataContext().GetConnectionAsync().Table<CalendarObject>().Where(e => e.Path == item.Reference && e.CalendarId == _calendar.CalendarId).FirstOrDefaultAsync();
                    if(fromDatabase == null) {

                    }
                    if (item.ETag.Length > 0) {
                        fromDatabase.CalendarData = item.CalendarData;
                        fromDatabase.ETag = item.ETag;
                        fromDatabase.InSync = true;
                        App.GetDataContext().StoreCalendarObjectAsync(fromDatabase);
                        fromDatabase.ParseCalendarData();
                    }
                }
            }
        }
    }
}
