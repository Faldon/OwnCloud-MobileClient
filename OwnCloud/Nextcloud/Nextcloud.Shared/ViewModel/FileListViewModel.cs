using Nextcloud.Data;
using Nextcloud.DAV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Notifications;

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
            IsFetching = false;
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
            foreach (File fileFromDatabase in _account.Files.Where(f => (f.Filepath.TrimEnd('/') == (_account.Server.WebDAVPath.TrimEnd('/') + _currentPath + f.Filename)) || ((_account.Server.WebDAVPath.TrimEnd('/') + _currentPath.TrimEnd('/')) == f.Filepath + f.Filename)).OrderBy(f => f.Filepath).ToList()) {
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
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { IsFetching = false; CleanupDatabase(); });
            } else {
                System.Diagnostics.Debug.WriteLine(result.Status);
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    IsFetching = false;
                    LastError = result.Request.LastException.Message;
                });
            }
        }

        public async void DeleteFile(File fileToDelete, bool remote = false) {
            IsFetching = true;
            string localpath = fileToDelete.Filepath.Replace(_account.Server.WebDAVPath.TrimEnd('/'), _account.ServerFQDN + "/" + _account.Username);
            if (!fileToDelete.IsDirectory) {
                try {
                    StorageFile f = await ApplicationData.Current.LocalFolder.GetFileAsync(localpath.Replace("/", "\\"));
                    await f.DeleteAsync();
                } catch (FileNotFoundException) {

                } finally {
                    fileToDelete.IsDownloaded = false;
                    App.GetDataContext().StoreFile(fileToDelete);
                    fileToDelete.NotifyPropertyChanged("IsDownloaded");
                    FileCollection.Where(f => f.Filename == fileToDelete.Filename && f.Filepath == fileToDelete.Filepath).First().IsDownloaded = false;
                    if (remote) {
                        NetworkCredential cred = await _account.GetCredential();
                        var webdav = new WebDAV(_account.GetWebDAVRoot(), cred);
                        webdav.StartRequest(DAVRequestHeader.Delete(fileToDelete.Filepath.Replace(_account.Server.WebDAVPath.TrimEnd('/'), "")), fileToDelete.Filename, DeleteFileComplete);
                    } else {
                        IsFetching = false;
                    }
                }
            } else {
                try {
                    StorageFolder f = await ApplicationData.Current.LocalFolder.GetFolderAsync(localpath.Replace("/", "\\"));
                    await f.DeleteAsync();
                } catch (FileNotFoundException) {

                } finally {
                    foreach (File f in FileCollection.Where(f => f.Filepath.StartsWith(fileToDelete.Filepath)).ToList()) {
                        f.IsDownloaded = false;
                        App.GetDataContext().StoreFile(f);
                        fileToDelete.NotifyPropertyChanged("IsDownloaded");
                    }
                    if (remote) {
                        NetworkCredential cred = await _account.GetCredential();
                        var webdav = new WebDAV(_account.GetWebDAVRoot(), cred);
                        webdav.StartRequest(DAVRequestHeader.Delete(fileToDelete.Filepath.Replace(_account.Server.WebDAVPath.TrimEnd('/'), "")), fileToDelete.Filename, DeleteFileComplete);
                    } else {
                        IsFetching = false;
                    }
                }
            }
        }

        private async void DeleteFileComplete(DAVRequestResult result, object userObj) {
            if (result.Status != ServerStatus.NoContent) {
                var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                xml.GetElementsByTagName("text").First().AppendChild(xml.CreateTextNode(String.Format(App.Localization().GetString("FileListPage_DeletionFailed"), (string)userObj)));
                xml.GetElementsByTagName("text").Last().AppendChild(xml.CreateTextNode(result.StatusText));
                ToastNotification toast = new ToastNotification(xml);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            } else {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    foreach (File f in FileCollection.Where(f => f.Filename == (string)userObj).ToList()) {
                        App.GetDataContext().RemoveFile(f);
                        FileCollection.Remove(f);
                    };
                    IsFetching = false;
                });
                var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                xml.GetElementsByTagName("text").First().AppendChild(xml.CreateTextNode(App.Localization().GetString("FileListPage_DeletionSuccessRemote")));
                xml.GetElementsByTagName("text").Last().AppendChild(xml.CreateTextNode((string)userObj));
                ToastNotification toast = new ToastNotification(xml);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
        }

        public async Task<bool> DownloadFileAsync(Uri uriToDownload, string fileName, DateTime lastModificationDate, CancellationToken cToken) {
            IsFetching = true;
            try {
                StorageFolder localStorage = ApplicationData.Current.LocalFolder;
                StorageFolder server = await localStorage.CreateFolderAsync(Servername, CreationCollisionOption.OpenIfExists);
                StorageFolder user = await server.CreateFolderAsync(Username, CreationCollisionOption.OpenIfExists);
                StorageFolder cwd = user;

                foreach (string folder in CurrentPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)) {
                    cwd = await cwd.CreateFolderAsync(folder, CreationCollisionOption.OpenIfExists);
                }

                StorageFile localFile;
                try {
                    localFile = await cwd.GetFileAsync(fileName);
                    if (localFile.DateCreated > lastModificationDate) {
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await Windows.System.Launcher.LaunchFileAsync(localFile));
                        return true;
                    }
                } catch (Exception) {
                    localFile = await cwd.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                }
                BackgroundDownloader loader = new BackgroundDownloader();
                var cred = await Account.GetCredential();
                loader.ServerCredential = new Windows.Security.Credentials.PasswordCredential(Servername, cred.UserName, cred.Password);
                DownloadOperation dl = loader.CreateDownload(uriToDownload, localFile);
                var dlOperation = await Task.Run(() => { return dl.StartAsync(); });

                dlOperation.Completed = async delegate (IAsyncOperationWithProgress<DownloadOperation, DownloadOperation> dlCompleted, AsyncStatus status) {
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { IsFetching = false; });
                    if (dlCompleted.ErrorCode == null) {
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await Windows.System.Launcher.LaunchFileAsync(localFile));
                    } else {
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { LastError = dlCompleted.ErrorCode.Message; });
                        await localFile.DeleteAsync();
                    }
                };
                return dlOperation.ErrorCode == null;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
                IsFetching = false;
                return false;
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

        private void CleanupDatabase() {
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
