using Nextcloud.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

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

        public bool DeleteAccount(Account account) {
            AccountCollection.Remove(account);
            App.GetDataContext().RemoveAccount(account);
            return true;
        }
    }
}
