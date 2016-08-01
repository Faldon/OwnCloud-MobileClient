using Nextcloud.Data;
using Nextcloud.DAV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Nextcloud.ViewModel
{
    class FileListViewModel : ViewModel
    {
        private CoreDispatcher dispatcher;
        private List<File> remoteFilesInCurrentPath;
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
        public Account Account
        {
            get
            {
                return _account;
            }
            set { }
        }

        public string Username
        {
            get
            {
                return _account.Username;
            }
            set { }
        }

        public string Servername
        {
            get
            {
                return _account.Server.FQDN;
            }
            set { }
        }

        public string WebDAVRoot
        {
            get
            {
                return _account.Server.WebDAVPath;
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
            remoteFilesInCurrentPath = new List<File>();
        }

        public async void StartFetching(string path=null)
        {
            CurrentPath = path ?? "/";
            IsFetching = true;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            NetworkCredential cred = await _account.GetCredential();
            var webdav = new WebDAV(_account.GetWebDAVRoot(), cred);
            FileCollection.Clear();            
            foreach (File fileFromDatabase in _account.Files.Where(f => (f.Filepath.TrimEnd('/') == (_account.Server.WebDAVPath.TrimEnd('/') + _currentPath + f.Filename)) || ((_account.Server.WebDAVPath.TrimEnd('/') + _currentPath.TrimEnd('/')) == f.Filepath + f.Filename)).ToList()) {
                FileCollection.Add(fileFromDatabase);
            }
            webdav.StartRequest(DAVRequestHeader.CreateListing(_currentPath), DAVRequestBody.CreateAllPropertiesListing(), null, FetchingComplete);
        }

        private async void FetchingComplete(DAVRequestResult result, object userObj)
        {
            if (result.Status == ServerStatus.MultiStatus && !result.Request.ErrorOccured && result.Items.Count > 0) {
                remoteFilesInCurrentPath.Clear();
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
                        File storedFile = _account.Files.Find(f => f.Filename == fileItem.Filename && f.Filepath == fileItem.Filepath);
                        if(storedFile == null || storedFile.ETag != fileItem.ETag) {
                            fileItem.IsDownloaded = false;
                            int id = App.GetDataContext().StoreFile(fileItem);
                            File updatedFile = App.GetDataContext().LoadFile(id);
                            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateFilelist(updatedFile));
                        }
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => remoteFilesInCurrentPath.Add(fileItem));
                    }
                }
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { IsFetching = false; CleanupFiles(); });
            } else {
                System.Diagnostics.Debug.WriteLine(result.Status);
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    IsFetching = false;
                    LastError = result.Request.LastException.Message;
                });
            }
        }

        private void UpdateFilelist(File updatedFile) {
            File currentFile = FileCollection.Where(f => f.FileId == updatedFile.FileId).FirstOrDefault();
            if(currentFile != null) {
                FileCollection.Remove(currentFile);
            }
            FileCollection.Add(updatedFile);
            NotifyPropertyChanged("FileCollection");
        }

        private void CleanupFiles() {
            string absoluteCurrentPath = _account.Server.WebDAVPath.TrimEnd('/') + _currentPath;
            List<File> filesInDatabase = App.GetDataContext().GetUserfilesInPath(_account, absoluteCurrentPath);
            filesInDatabase = filesInDatabase.Where(f => f.ParentFilepath == absoluteCurrentPath || (f.Filepath + f.Filename) == absoluteCurrentPath.TrimEnd('/')).ToList();
            foreach(File storedFile in filesInDatabase) {
                if (remoteFilesInCurrentPath.Where(f => storedFile.Filename == f.Filename).FirstOrDefault() == null) {
                    App.GetDataContext().RemoveFile(storedFile);
                    FileCollection.Remove(FileCollection.Where(f => f.FileId == storedFile.FileId).First());
                    NotifyPropertyChanged("FileCollection");
                };
            }
            _account = App.GetDataContext().LoadAccount(_account.AccountId);
        }
    }
}
