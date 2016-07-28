using Nextcloud.Data;
using Nextcloud.DAV;
using System;
using System.Collections.ObjectModel;
using System.Net;
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
            //StartFetching();
        }

        public async void StartFetching(string path=null)
        {
            CurrentPath = path ?? "/";
            IsFetching = true;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            NetworkCredential cred = await _account.GetCredential();
            var webdav = new WebDAV(_account.GetWebDAVRoot(), cred);
            FileCollection.Clear();
            webdav.StartRequest(DAVRequestHeader.CreateListing(_currentPath), DAVRequestBody.CreateAllPropertiesListing(), null, FetchingComplete);
        }

        private async void FetchingComplete(DAVRequestResult result, object userObj)
        {
            if (result.Status == ServerStatus.MultiStatus && !result.Request.ErrorOccured && result.Items.Count > 0) {
                bool _firstItem = false;
                foreach (DAVRequestResult.Item item in result.Items) {
                    File fileItem = new File()
                    {
                        Filename = item.LocalReference,
                        Filepath = item.Reference,
                        Filesize = item.ContentLength,
                        Filetype = item.ContentType,
                        FileCreated = item.CreationDate,
                        FileLastModified = item.LastModified,
                        ETag = item.ETag,
                        IsDirectory = (item.ResourceType == ResourceType.Collection),
                        AccountId = _account.AccountId,
                        Account = _account
                    };

                    bool display = true;
                    if (!_firstItem) {
                        _firstItem = true;

                        // Root Folder
                        if (fileItem.IsDirectory) {
                            if (item.Reference == _account.Server.WebDAVPath) {
                                // WebDAV Root
                                display = false;
                            } else {
                                fileItem.IsRootItem = true;
                                fileItem.Filepath = fileItem.ParentFilepath;
                            }
                        }
                    }
                    if(display) {
                        App.GetDataContext().StoreFile(fileItem);
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => FileCollection.Add(fileItem));
                    }
                }
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => IsFetching = false);
            } else {
                System.Diagnostics.Debug.WriteLine(result.Status);
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    IsFetching = false;
                    LastError = result.Request.LastException.Message;
                    foreach(File fileFromDatabase in _account.Files) {
                        if(fileFromDatabase.Filepath.StartsWith(_account.Server.WebDAVPath.TrimEnd('/')+_currentPath)) {
                            FileCollection.Add(fileFromDatabase);
                        }    
                    }
                });
                //await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => LastError = result.StatusText);
            }
        }
    }
}
