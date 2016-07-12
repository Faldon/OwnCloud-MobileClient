using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Nextcloud.Data;

namespace Nextcloud.ViewModel
{
    class AccountViewModel : IViewModel
    {
        private Account _account;
        private Server _server;

        public string Username { get { return _account.Username; } set { _account.Username=value; } }
        public string Password { get { return _account.Password; } set { _account.Password=value; } }
        public string Servername { get { return _server.FQDN; } set { _server.FQDN = value; } }
        public bool UseSSL {
            get {
                return _server.Protocol == "https";
            }
            set {
                if (value) {
                    _server.Protocol = "https";
                } else {
                    _server.Protocol = "http";
                }
            }
        }
        public bool EnableCalendar { get { return _account.IsCalendarEnabled; } set { _account.IsCalendarEnabled = value; } }

        public AccountViewModel(Account currentAccount) {
            _account = currentAccount;
            _server = _account.Server ?? new Server {
                Accounts = new List<Account> { _account }
            };
        }

        public void SaveAccount() {
            App.GetDatacontext().StoreAccount(_account);
        }
    }
}
