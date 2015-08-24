using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using OwnCloud.Data;
using OwnCloud.Data.DAV;
using OwnCloud.View.Controls;
using OwnCloud.Extensions;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Foundation.Collections;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace OwnCloud.View.Page
{
    public partial class RemoteFiles : PhoneApplicationPage
    {

        private Account _workingAccount;
        private FileListDataContext _context;
        private IsolatedStorageFile _localStorage;
        private string[] _views = { "detail", "tile" };
        private string _workingPath = "";
        private DAVRequestResult.Item _item;
        private bool _deletionSuccess = true;

        public RemoteFiles()
        {
            InitializeComponent();
            _context = new FileListDataContext();
            DataContext = _context;

            var appBar = this.Resources["SelectFilesAppBar"] as ApplicationBar;
            appBar.TranslateButtons();
            ApplicationBarMenuItem selectAllMenuItem = new ApplicationBarMenuItem(Resource.Localization.AppResources.ApplicationBarMenuItem_SelectAll);
            selectAllMenuItem.Click += selectAllMenuItems;
            ApplicationBarMenuItem unselectAllMenuItem = new ApplicationBarMenuItem(Resource.Localization.AppResources.ApplicationBarMenuItem_UnSelectAll);
            unselectAllMenuItem.Click += unselectAllMenuItems;
            appBar.MenuItems.Add(selectAllMenuItem);
            appBar.MenuItems.Add(unselectAllMenuItem);
        }

        void selectAllMenuItems(object sender, EventArgs e)
        {
            foreach (FileDetailViewControl detailControl in DetailList.Items)
            {
                detailControl.FileCheckbox.IsChecked = true;
            }
        }

        void unselectAllMenuItems(object sender, EventArgs e)
        {
            foreach (FileDetailViewControl detailControl in DetailList.Items)
            {
                detailControl.FileCheckbox.IsChecked = false;
            }
        }

        private void ToggleTray()
        {
            Dispatcher.BeginInvoke(() =>
            {
                SystemTray.SetIsVisible(this, !SystemTray.IsVisible);
            });
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _workingAccount = App.DataContext.LoadAccount(NavigationContext.QueryString["account"]);
                _workingAccount.RestoreCredentials();
                _localStorage = App.DataContext.Storage;

            }
            catch (Exception)
            {

            }
            ApplicationBar = (ApplicationBar)Resources["DefaultAppBar"];
            ApplicationBar.TranslateButtons();
            FetchStructure(_workingAccount.WebDAVPath);
        }

        private void SelectFile(object sender, EventArgs e)
        {
            FileOpenPicker opener = new FileOpenPicker();
            opener.ViewMode = PickerViewMode.Thumbnail;
            opener.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            opener.FileTypeFilter.Add("*");
            opener.ContinuationData["Operation"] = "Fileupload";
            opener.ContinuationData["UploadPath"] = _workingPath;
            opener.PickMultipleFilesAndContinue();

        }

        private void CheckItems(object sender, EventArgs e)
        {
            ApplicationBar = this.Resources["SelectFilesAppBar"] as ApplicationBar;

            DetailList.SelectionMode = SelectionMode.Multiple;
            foreach (FileDetailViewControl detailControl in DetailList.Items)
            {
                detailControl.Tap -= FileItem_Tapped;
                detailControl.FileItemCheckbox.Width = new GridLength(60);
                if (detailControl.FileProperties.IsRootItem)
                {
                    detailControl.FileCheckbox.Visibility = System.Windows.Visibility.Collapsed;
                    detailControl.FileCheckbox.IsEnabled = false;
                }
            }

        }

        private void SelectionCancel(object sender, EventArgs e)
        {
            ApplicationBar = this.Resources["DefaultAppBar"] as ApplicationBar;
            foreach (FileDetailViewControl detailControl in DetailList.Items)
            {
                detailControl.Tap += FileItem_Tapped;
                detailControl.FileItemCheckbox.Width = new GridLength(0);
            }

        }

        private void SelectionDelete(object sender, EventArgs e)
        {
            var box = new CustomMessageBox()
            {
                Caption = Resource.Localization.AppResources.DeleteFiles_Caption,
                Message = Resource.Localization.AppResources.DeleteFiles_Message,
                LeftButtonContent = Resource.Localization.AppResources.Button_OK,
                RightButtonContent = Resource.Localization.AppResources.Button_Cancel,
                IsFullScreen = false
            };
            box.Dismissed += (s, deleteFiles_MessageBox) =>
            {
                if (deleteFiles_MessageBox.Result == CustomMessageBoxResult.LeftButton)
                {
                    var checkedItems = DetailList.Items.Cast<FileDetailViewControl>().Where(t => (bool)t.FileCheckbox.IsChecked);
                    foreach (FileDetailViewControl detailControl in checkedItems)
                    {

                        if ((bool)detailControl.FileCheckbox.IsChecked)
                        {
                            var path = detailControl.FileProperties.FilePath;

                            var fileName = detailControl.FileProperties.FileName;
                            var localPath = _workingAccount.ServerDomain + "\\" + _workingAccount.DisplayUserName + "\\" + _workingPath.Remove(0, _workingAccount.WebDAVPath.Length).Replace("/", "\\");
                            var userFile = localPath + fileName;

                            if (_localStorage.FileExists(userFile))
                            {
                                _localStorage.DeleteFile(userFile);
                            }
                            else if (_localStorage.DirectoryExists(userFile))
                            {
                                _localStorage.DeleteDirectory(userFile);
                            }

                            var dav = new WebDAV(_workingAccount.GetUri(), _workingAccount.GetCredentials());
                            dav.StartRequest(DAVRequestHeader.Delete(path), checkedItems.Last() == detailControl, SelectionDeleteComplete);
                        }
                    }
                }
            };
            box.Show();
        }

        private void SelectionDeleteComplete(DAVRequestResult result, object userObj)
        {
            if (result.Status != ServerStatus.NoContent)
            {
                _deletionSuccess = false;
            }
            if ((bool)userObj == true)
            {
                switch (_deletionSuccess)
                {
                    case true:
                        Dispatcher.BeginInvoke(new Action(() => FetchStructure(_workingPath)));
                        Dispatcher.BeginInvoke(new Action(() => SelectionCancel(this, null)));
                        break;
                    case false:
                        Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(Resource.Localization.AppResources.ItemDeletion_Error)));
                        Dispatcher.BeginInvoke(new Action(() => FetchStructure(_workingPath)));
                        Dispatcher.BeginInvoke(new Action(() => SelectionCancel(this, null)));
                        _deletionSuccess = true;
                        break;
                }
            }

        }

        private void NewFolder(object sender, EventArgs e)
        {
            var folderName = new TextBox();
            var box = new CustomMessageBox()
            {
                Caption = Resource.Localization.AppResources.NewFolder_Caption,
                Message = Resource.Localization.AppResources.NewFolder_Message,
                LeftButtonContent = Resource.Localization.AppResources.Button_OK,
                RightButtonContent = Resource.Localization.AppResources.Button_Cancel,
                Content = folderName,
                IsFullScreen = false
            };
            box.Dismissed += (s, newFolder_MessageBox) =>
            {
                if (newFolder_MessageBox.Result == CustomMessageBoxResult.LeftButton)
                {
                    var path = _workingPath + "/" + folderName.Text;
                    var dav = new WebDAV(_workingAccount.GetUri(), _workingAccount.GetCredentials());
                    dav.StartRequest(DAVRequestHeader.MakeCollection(path), folderName.Text, NewFolderComplete);
                }
            };
            box.Show();
        }

        private void NewFolderComplete(DAVRequestResult result, object userObj)
        {
            if (result.Status == ServerStatus.Created)
            {
                Dispatcher.BeginInvoke(new Action(() => FetchStructure(_workingPath)));
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(String.Format(Resource.Localization.AppResources.NewFolder_Error, (string)userObj))));
            }
        }

        /// <summary>
        /// Tries to fetch a given path and refreshes the views.
        /// </summary>
        /// <param name="path"></param>
        private void FetchStructure(string path)
        {
            _workingPath = path;

            progress.IsVisible = true;
            progress.Text = Resource.Localization.AppResources.RemoteFilesPage_FetchingStructure;

            DetailList.Show();
            // fadeout existing from detail view
            if (DetailList.Items.Count == 0)
            {
                StartRequest();
            }
            else
            {
                int detailItemsLeft = DetailList.Items.Count;
                foreach (FrameworkElement item in DetailList.Items)
                {
                    item.FadeOut(100, () =>
                    {
                        --detailItemsLeft;
                        if (detailItemsLeft <= 0)
                        {
                            DetailList.Items.Clear();
                            StartRequest();
                        }
                    });
                }
            }

            BreadCrumb.Text = _workingPath.Substring(_workingAccount.WebDAVPath.Length - 1);
        }

        private void StartRequest()
        {
            var dav = new WebDAV(_workingAccount.GetUri(), _workingAccount.GetCredentials());
            dav.StartRequest(DAVRequestHeader.CreateListing(_workingPath), DAVRequestBody.CreateAllPropertiesListing(), null, FetchStructureComplete);
        }

        private void FetchStructureComplete(DAVRequestResult result, object userObj)
        {
            if (result.Status == ServerStatus.MultiStatus && !result.Request.ErrorOccured && result.Items.Count > 0)
            {
                bool _firstItem = false;
                // display all items linear
                // we cannot wait till an item is displayed, instead for a fluid
                // behaviour we should calculate fadeIn-delays.
                int delayStart = 0;
                int delayStep = 50; // ms

                foreach (DAVRequestResult.Item item in result.Items)
                {
                    File fileItem = new File()
                    {
                        FileName = item.LocalReference,
                        FilePath = item.Reference,
                        FileSize = item.ContentLength,
                        FileType = item.ContentType,
                        FileCreated = item.CreationDate,
                        FileLastModified = item.LastModified,
                        IsDirectory = item.ResourceType == ResourceType.Collection
                    };

                    bool display = true;

                    Dispatcher.BeginInvoke(() =>
                    {
                        if (!_firstItem)
                        {
                            _firstItem = true;

                            // Root
                            if (fileItem.IsDirectory)
                            {
                                if (item.Reference == _workingAccount.WebDAVPath)
                                {
                                    // cannot go up further
                                    display = false;
                                }
                                else
                                {
                                    fileItem.IsRootItem = true;
                                    fileItem.FilePath = fileItem.FileParentPath;
                                }
                            }
                        }

                        if (display)
                        {
                            FileDetailViewControl detailControl = new FileDetailViewControl()
                                    {
                                        DataContext = fileItem,
                                        Opacity = 0,
                                        Background = new SolidColorBrush() { Color = Colors.Transparent },
                                    };
                            detailControl.Tap += FileItem_Tapped;

                            DetailList.Items.Add(detailControl);
                            detailControl.Delay(delayStart, () =>
                            {
                                detailControl.FadeIn(100);
                            });
                            delayStart += delayStep;
                        }
                    });
                }

                Dispatcher.BeginInvoke(() =>
                {
                    progress.IsVisible = false;
                });
            }
            else
            {
                Dispatcher.BeginInvoke(() =>
                {
                    progress.IsVisible = false;
                    if (result.Status == ServerStatus.Unauthorized)
                    {
                        MessageBox.Show("FetchFile_Unauthorized".Translate(), "Error_Caption".Translate(), MessageBoxButton.OK);
                    }
                    else
                    {
                        MessageBox.Show("FetchFile_Unexpected_Result".Translate(), "Error_Caption".Translate(), MessageBoxButton.OK);
                    }
                });
            }
        }

        void FileItem_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var item = (FileDetailViewControl)sender;
            item.RotateSyncButton.Begin();
            OpenItem(item);
        }

        private async void OpenItem(FileDetailViewControl itemControl)
        {
            File item = itemControl.FileProperties;
            if (item.IsDirectory)
            {
                itemControl.RotateSyncButton.Stop();
                FetchStructure(item.FilePath);
            }
            else
            {
                // Download file to isolated storage
                var downloaded = await DownloadFileAsync(_workingAccount.GetUri(item.FilePath), item.FileName, item.FileLastModified, new CancellationToken(false));
                if (downloaded)
                {
                    var path = _workingAccount.ServerDomain + "\\" + _workingAccount.DisplayUserName + "\\" + _workingPath.Remove(0, _workingAccount.WebDAVPath.Length).Replace("/", "\\") + item.FileName;
                    StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(path);
                    itemControl.RotateSyncButton.Stop();
                    var success = await Windows.System.Launcher.LaunchFileAsync(file);
                    DetailList.SelectedIndex = -1;
                }
            }
        }

        private async Task<bool> DownloadFileAsync(Uri uriToDownload, string fileName, DateTime lastModificationDate, CancellationToken cToken)
        {
            try
            {
                var localPath = _workingAccount.ServerDomain + "\\" + _workingAccount.DisplayUserName + "\\" + _workingPath.Remove(0, _workingAccount.WebDAVPath.Length).Replace("/", "\\");
                var userFile = localPath + fileName;
                if (_localStorage.FileExists(userFile))
                {
                    var lastWriteDate = _localStorage.GetLastWriteTime(userFile);
                    if (lastWriteDate > lastModificationDate) return true;
                }
                if (!_localStorage.DirectoryExists(localPath))
                {
                    _localStorage.CreateDirectory(localPath);
                }
                using (System.IO.Stream mystr = await DownloadFile(uriToDownload, _workingAccount.GetCredentials()))
                using (IsolatedStorageFileStream file = _localStorage.CreateFile(userFile))
                {
                    const int BUFFER_SIZE = 1024;
                    byte[] buf = new byte[BUFFER_SIZE];

                    int bytesread = 0;
                    while ((bytesread = await mystr.ReadAsync(buf, 0, BUFFER_SIZE)) > 0)
                    {
                        cToken.ThrowIfCancellationRequested();
                        file.Write(buf, 0, bytesread);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }

        private async Task<bool> UploadFilesAsync(Uri url, NetworkCredential credentials, String uploadDir, StorageFile[] files)
        {
            try
            {
                var tcs = new TaskCompletionSource<System.IO.Stream>();
                var request = HttpWebRequest.Create(url);
                request.Credentials = credentials;
                request.Method = "POST";
                //var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
                //request.ContentType = "multipart/form-data; boundary=" + boundary;
                //boundary = "--" + boundary;

                //System.IO.Stream requestStream = await request.GetRequestStreamAsync();

                //// Write the values
                //var buffer = Encoding.UTF8.GetBytes(boundary + Environment.NewLine);

                //requestStream.Write(buffer, 0, buffer.Length);
                //buffer = Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"{1}{1}", "dir", Environment.NewLine));
                //requestStream.Write(buffer, 0, buffer.Length);
                //buffer = Encoding.UTF8.GetBytes(uploadDir + Environment.NewLine);
                //requestStream.Write(buffer, 0, buffer.Length);


                //// Write the files
                //foreach (var file in files)
                //{
                //    var filebuffer = Encoding.UTF8.GetBytes(boundary + Environment.NewLine);
                //    requestStream.Write(filebuffer, 0, filebuffer.Length);
                //    filebuffer = Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"{2}", "file", file.Name, Environment.NewLine));
                //    requestStream.Write(filebuffer, 0, filebuffer.Length);
                //    filebuffer = Encoding.UTF8.GetBytes(string.Format("Content-Type: {0}{1}{1}", file.ContentType, Environment.NewLine));
                //    requestStream.Write(filebuffer, 0, filebuffer.Length);

                //    var fileStream = await file.OpenAsync(FileAccessMode.Read);
                //    var reader = new Windows.Storage.Streams.DataReader(fileStream.GetInputStreamAt(0));
                //    var bytes = new byte[fileStream.Size];
                //    await reader.LoadAsync((uint)fileStream.Size);
                //    reader.ReadBytes(bytes);
                //    var fs = new System.IO.MemoryStream(bytes);
                //    fs.CopyTo(requestStream);

                //    filebuffer = Encoding.UTF8.GetBytes(Environment.NewLine);
                //    requestStream.Write(filebuffer, 0, filebuffer.Length);
                //}

                //var boundaryBuffer = Encoding.UTF8.GetBytes(boundary + "--");
                //requestStream.Write(boundaryBuffer, 0, boundaryBuffer.Length);
                request.BeginGetResponse(async (result) =>
                {
                    try
                    {
                        HttpWebRequest r = (HttpWebRequest)result.AsyncState;
                        using (System.IO.Stream requestStream = request.EndGetRequestStream(result))
                        {
                            var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
                            request.ContentType = "multipart/form-data; boundary=" + boundary;
                            boundary = "--" + boundary;

                            //System.IO.Stream requestStream = await request.GetRequestStreamAsync();

                            // Write the values
                            var buffer = Encoding.UTF8.GetBytes(boundary + Environment.NewLine);

                            requestStream.Write(buffer, 0, buffer.Length);
                            buffer = Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"{1}{1}", "dir", Environment.NewLine));
                            requestStream.Write(buffer, 0, buffer.Length);
                            buffer = Encoding.UTF8.GetBytes(uploadDir + Environment.NewLine);
                            requestStream.Write(buffer, 0, buffer.Length);


                            // Write the files
                            foreach (var file in files)
                            {
                                var filebuffer = Encoding.UTF8.GetBytes(boundary + Environment.NewLine);
                                requestStream.Write(filebuffer, 0, filebuffer.Length);
                                filebuffer = Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"{2}", "file", file.Name, Environment.NewLine));
                                requestStream.Write(filebuffer, 0, filebuffer.Length);
                                filebuffer = Encoding.UTF8.GetBytes(string.Format("Content-Type: {0}{1}{1}", file.ContentType, Environment.NewLine));
                                requestStream.Write(filebuffer, 0, filebuffer.Length);

                                var fileStream = await file.OpenAsync(FileAccessMode.Read);
                                var reader = new Windows.Storage.Streams.DataReader(fileStream.GetInputStreamAt(0));
                                var bytes = new byte[fileStream.Size];
                                await reader.LoadAsync((uint)fileStream.Size);
                                reader.ReadBytes(bytes);
                                var fs = new System.IO.MemoryStream(bytes);
                                fs.CopyTo(requestStream);

                                filebuffer = Encoding.UTF8.GetBytes(Environment.NewLine);
                                requestStream.Write(filebuffer, 0, filebuffer.Length);
                            }

                            var boundaryBuffer = Encoding.UTF8.GetBytes(boundary + "--");
                            requestStream.Write(boundaryBuffer, 0, boundaryBuffer.Length);
                        }
                        request.BeginGetResponse(a =>
                        {
                            try
                            {
                                var response = request.EndGetResponse(a);
                                var responseStream = response.GetResponseStream();
                                using (var sr = new System.IO.StreamReader(responseStream))
                                {
                                    {
                                        using (System.IO.StreamReader streamReader = new System.IO.StreamReader(response.GetResponseStream()))
                                        {
                                            string responseString = streamReader.ReadToEnd();
                                        }
                                    }
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }, null);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }, request);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return false;
            }
        }

        private static Task<System.IO.Stream> DownloadFile(Uri url, NetworkCredential credentials)
        {
            var tcs = new TaskCompletionSource<System.IO.Stream>();
            var wc = new WebClient();
            wc.Credentials = credentials;
            wc.OpenReadCompleted += (s, e) =>
            {
                if (e.Error != null) tcs.TrySetException(e.Error);
                else if (e.Cancelled) tcs.TrySetCanceled();
                else tcs.TrySetResult(e.Result);
            };
            wc.OpenReadAsync(url);
            return tcs.Task;
        }

        public async void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args)
        {
            if ((args.ContinuationData["Operation"] as string) == "Fileupload" &&
                args.Files != null &&
                args.Files.Count > 0)
            {
                //var url = _workingAccount.GetUri("/index/files/ajax/upload.php");
                var url = _workingAccount.GetUri(_workingPath);
                //StorageFile[] files = new StorageFile[args.Files.ToArray];


                //foreach (StorageFile file in args.Files) {
                //    files[files.Length] = file;
                //}

                var success = await UploadFilesAsync(url, _workingAccount.GetCredentials(), args.ContinuationData["UploadPath"] as string, (StorageFile[])args.Files.ToArray());
                FetchStructure(_workingPath);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var app = App.Current as App;
            if (app.FilePickerContinuationArgs != null)
            {
                this.ContinueFileOpenPicker(app.FilePickerContinuationArgs);
            }
        }
    }
}