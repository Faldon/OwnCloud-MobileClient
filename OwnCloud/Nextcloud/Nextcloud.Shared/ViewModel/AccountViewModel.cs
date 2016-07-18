using System.Collections.Generic;
using Nextcloud.Data;

namespace Nextcloud.ViewModel {
    class AccountViewModel : ViewModel {
        private Account _account;

        private string _username;
        public string Username
        {
            get { return _username; }
            set
            {
                _username = value;
                NotifyPropertyChanged();
            }
        }
        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                NotifyPropertyChanged();
            }
        }
        public string Servername
        {
            get { return _account.Server.FQDN; }
            set
            {
                _account.Server.FQDN = value;
                NotifyPropertyChanged();
            }
        }
        public bool UseSSL
        {
            get
            {
                return _account.Server.Protocol == "https";
            }
            set
            {
                if (value) {
                    _account.Server.Protocol = "https";
                } else {
                    _account.Server.Protocol = "http";
                }
                NotifyPropertyChanged();
            }
        }
        public bool EnableCalendar
        {

            get { return _account.IsCalendarEnabled; }
            set
            {
                _account.IsCalendarEnabled = value;
                NotifyPropertyChanged();
            }
        }

        private bool _checkingConnection;
        public string CheckingConnection {
            get
            {
                return _checkingConnection ? "Visible" : "Collapsed";
            }
            set
            {
                _checkingConnection = value == "Visible";
            } 
        }

        public AccountViewModel(Account currentAccount) {
            _checkingConnection = false;
            _account = currentAccount;
            if (_account.Server == null) {
                _account.Server = new Server() {
                    Accounts = new List<Account> { _account }
                };
            };
            LoadAccount();
        }

        public async void SaveAccount() {
            _checkingConnection = true;
            OnPropertyChanged("CheckingConnection");
            _account.Username = await Utility.EncryptString(_username);
            _account.Password = await Utility.EncryptString(_password);
            //App.GetDatacontext().StoreAccount(_account);
        }

        public async void LoadAccount() {
            _username = await Utility.DecryptString(_account.Username);
            _password = await Utility.DecryptString(_account.Password);
            OnPropertyChanged("");
        }

        public bool CanSave() {
            return (_username.Length != 0 && _password.Length != 0 && Servername.Length != 0);
        }

        public bool CanCancel() {
            return App.GetDatacontext().GetConnection().Table<Account>().Count() > 0;
        }
    }
}
