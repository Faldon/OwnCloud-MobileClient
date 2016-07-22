using Nextcloud.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Nextcloud.ViewModel
{
    class FileListViewModel : ViewModel
    {
        private ObservableCollection<File> _fileCollection;
        public ObservableCollection<File> FileCollection
        {
            get
            {
                return _fileCollection;
            }
            set
            {
                _fileCollection = value;
                NotifyPropertyChanged();
            }
        }

        private Account _account;
        public string Username
        {
            get { return _account.Username; }
            set { }
        }

        public FileListViewModel(Account account) {
            _account = account;
            _fileCollection = new ObservableCollection<File>(_account.Files);
        }
    }
}
