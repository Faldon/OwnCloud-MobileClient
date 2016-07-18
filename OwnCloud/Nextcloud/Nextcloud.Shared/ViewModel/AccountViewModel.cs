using System.Collections.Generic;
using Nextcloud.Data;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Nextcloud.ViewModel {
    class AccountViewModel : ViewModel {
        private Account _account;

        public string Username
        {
            get { return _account.Username; }
            set
            {
                _account.Username = value;
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

        public AccountViewModel(Account currentAccount) {
            _account = currentAccount;
            if (_account.Server == null) {
                _account.Server = new Server() {
                    Accounts = new List<Account> { _account }
                };
            };
            LoadAccount();
        }

        public async Task<bool> SaveAccount() {
            _account.Password = await Utility.EncryptString(_password);
            App.GetDataContext().StoreAccount(_account);
            return true;
        }

        public async void LoadAccount() {
            _password = await Utility.DecryptString(_account.Password);
            OnPropertyChanged("");
        }

        public bool CanSave() {
            return (Username.Length != 0 && _password.Length != 0 && Servername.Length != 0);
        }

        public bool CanCancel() {
            return App.GetDataContext().GetConnection().Table<Account>().Count() > 0;
        }

        public Uri GetWebDAVRoot()
        {
            return new Uri(_account.Server.Protocol + "://" + _account.Server.FQDN.TrimEnd('/') + _account.Server.WebDAVPath, UriKind.Absolute);
        }

        public NetworkCredential GetCredential()
        {
            return new NetworkCredential(Username, _password);
        }
    }
}
