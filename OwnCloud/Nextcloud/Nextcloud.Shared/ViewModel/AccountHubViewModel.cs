using Nextcloud.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Nextcloud.ViewModel
{
    class AccountHubViewModel : ViewModel
    {
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

        public AccountHubViewModel(List<Account> accountList)
        {
            _accountCollection = new ObservableCollection<Account>(accountList);
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
    }
}
