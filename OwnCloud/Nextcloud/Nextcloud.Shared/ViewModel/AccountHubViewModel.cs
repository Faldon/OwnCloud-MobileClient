using Nextcloud.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

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
    }
}
