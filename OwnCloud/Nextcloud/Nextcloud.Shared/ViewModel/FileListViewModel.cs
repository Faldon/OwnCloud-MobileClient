using Nextcloud.Data;
using Nextcloud.DAV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using Windows.UI.Core;

namespace Nextcloud.ViewModel
{
    class FileListViewModel : ViewModel
    {
        private CoreDispatcher dispatcher;
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
            get
            {
                return _account.Username;
            }
            set { }
        }

        private string _currentPath;
        public string CurrentPath
        {
            get
            {
                return _currentPath;
            }
            set
            {
                _currentPath = value;
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

        public FileListViewModel(Account account)
        {
            _account = account;
            _currentPath = "/";
            _fileCollection = new ObservableCollection<File>();
            StartFetching();
        }


        private async void StartFetching()
        {
            IsFetching = true;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            NetworkCredential cred = await _account.GetCredential();
            var dav = new WebDAV(_account.GetWebDAVRoot(), cred);
            dav.StartRequest(DAVRequestHeader.CreateListing(_currentPath), DAVRequestBody.CreateAllPropertiesListing(), null, FetchingComplete);
        }

        private async void FetchingComplete(DAVRequestResult result, object userObj)
        {
            if (result.Status == ServerStatus.MultiStatus && !result.Request.ErrorOccured && result.Items.Count > 0) {
                bool _firstItem = false;
                // display all items linear
                // we cannot wait till an item is displayed, instead for a fluid
                // behaviour we should calculate fadeIn-delays.
                int delayStart = 0;
                int delayStep = 50; // ms

#if DEBUG
                App.GetDataContext().GetConnection().DeleteAll(typeof(File));
#endif
                foreach (DAVRequestResult.Item item in result.Items) {
                    File fileItem = new File()
                    {
                        Filename = item.LocalReference,
                        Filepath = item.Reference,
                        Filesize = item.ContentLength,
                        Filetype = item.ContentType,
                        FileCreated = item.CreationDate,
                        FileLastModified = item.LastModified,
                        IsDirectory = (item.ResourceType == ResourceType.Collection),
                        Account = _account
                    };

                    bool display = true;
                    if (!_firstItem) {
                        _firstItem = true;

                        // Root
                        if (fileItem.IsDirectory) {
                            if (item.Reference == _account.Server.WebDAVPath) {
                                // cannot go up further
                                display = false;
                            } else {
                                fileItem.IsRootItem = true;
                                fileItem.Filepath = fileItem.ParentFilepath;
                            }
                        }
                    }
                    if(display) {
                        //if(!App.GetDataContext().IsFileFetched(fileItem)) {
                        App.GetDataContext().StoreFile(fileItem);
                        //}
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FileCollection.Add(fileItem));
                    }
                    
                    //FileCollection.Add(fileItem);
                }
                //await dispatcher.RunAsync(CoreDispatcherPriority.Low, () => App.GetDataContext().UpdateFileTable(FileCollection));

            } else {
                _lastError = result.StatusText;
                //Dispatcher.BeginInvoke(() =>
                //{
                //    progress.IsVisible = false;
                //    if (result.Status == ServerStatus.Unauthorized) {
                //        MessageBox.Show("FetchFile_Unauthorized".Translate(), "Error_Caption".Translate(), MessageBoxButton.OK);
                //    } else {
                //        MessageBox.Show("FetchFile_Unexpected_Result".Translate(), "Error_Caption".Translate(), MessageBoxButton.OK);
                //    }
                //});
            }
        }
    }
}
