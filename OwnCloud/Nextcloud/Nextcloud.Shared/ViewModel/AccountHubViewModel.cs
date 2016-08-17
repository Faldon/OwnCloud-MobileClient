using Nextcloud.Data;
using Nextcloud.DAV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;

namespace Nextcloud.ViewModel
{
    class AccountHubViewModel : ViewModel
    {
        private CoreDispatcher dispatcher;
        private ObservableCollection<Account> _accountCollection;
        public ObservableCollection<Account> AccountCollection
        {
            get
            {
                return _accountCollection;
            }
            set
            {
                _accountCollection = value;
                NotifyPropertyChanged();
            }
        }
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

        public AccountHubViewModel(List<Account> accountList)
        {
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            AccountCollection = new ObservableCollection<Account>(accountList);
            CalendarCollection = new ObservableCollection<Calendar>();
            foreach (Account account in AccountCollection.Where(a => a.IsCalendarEnabled).ToList()) {
                StartFetchingCalendarAsync(account);
            }
        }

        public async Task<bool> DeleteAccount(Account account) {
            AccountCollection.Remove(account);
            try {
                StorageFolder localStorage = ApplicationData.Current.LocalFolder;
                StorageFolder server = await localStorage.GetFolderAsync(account.ServerFQDN);
                StorageFolder user = await server.GetFolderAsync(account.Username);
                await user.DeleteAsync();
            } catch (Exception) {

            }
            App.GetDataContext().RemoveAccount(account);
            return true;
        }

        private async void StartFetchingCalendarAsync(Account account) {
            var cred = await account.GetCredential();
            WebDAV dav = new WebDAV(account.GetCalDAVRoot(), cred);
            DAVRequestHeader header = new DAVRequestHeader(DAVRequestHeader.Method.PropertyFind, "/");
            header.Headers.Add(Header.Depth, HeaderAttribute.MethodDepth.ApplyResourceOnlyNoRoot);
            dav.StartRequest(header, DAVRequestBody.CreateCalendarPropertiesListening(), account, FetchingCalendarAsyncComplete);
        }

        private async void FetchingCalendarAsyncComplete(DAVRequestResult result, object userObj) {
            if (result.Status == ServerStatus.MultiStatus && !result.Request.ErrorOccured && result.Items.Count > 0) {
                Account _account = userObj as Account;
                foreach (DAVRequestResult.Item item in result.Items) {
                    if (item.Properties != null && !item.FailedProperties.Select(ps => ps.Status).Contains(ServerStatus.NotFound)) {
                        Calendar calendarItem = new Calendar() {
                            DisplayName = item.DisplayName,
                            Path = item.Reference,
                            CTag = item.CTag,
                            Color = item.CalendarColor,
                            AccountId = _account.AccountId,
                            Account = _account
                        };
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CalendarCollection.Add(calendarItem));
                    }
                }
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { SyncDatabaseAsync(); });
            } else {
                foreach (Calendar calendarFromDatabase in App.GetDataContext().GetConnection().Table<Calendar>().ToList()) {
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CalendarCollection.Add(calendarFromDatabase));
                }
            }
        }

        private async void SyncDatabaseAsync() {
            List<Calendar> calendarsInDatabase = await App.GetDataContext().GetConnectionAsync().Table<Calendar>().ToListAsync();
            foreach(Calendar remoteCalendar in CalendarCollection) {
                var localCalendar = calendarsInDatabase.Where(c => c.Path == remoteCalendar.Path).FirstOrDefault();
                if (localCalendar==null) {
                    remoteCalendar.IsSynced = true;
                    App.GetDataContext().StoreCalendarAsync(remoteCalendar);
                } else {
                    remoteCalendar.CalendarId = localCalendar.CalendarId;
                    remoteCalendar.IsSynced = localCalendar.CTag == remoteCalendar.CTag;
                    App.GetDataContext().UpdateCalendarAsync(remoteCalendar);
                }
            }
            foreach (Calendar localCalendar in calendarsInDatabase) {
                var remoteCalendar = CalendarCollection.Where(c => c.Path == localCalendar.Path).FirstOrDefault();
                if (remoteCalendar == null) {
                    App.GetDataContext().RemoveCalendarAsync(remoteCalendar);
                }
            }
        }
    }
}
