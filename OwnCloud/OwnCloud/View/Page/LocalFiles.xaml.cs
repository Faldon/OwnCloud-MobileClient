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
    public partial class LocalFiles : PhoneApplicationPage
    {

        private Account _workingAccount;
        private FileListDataContext _context;
        private IsolatedStorageFile _localStorage;
        private string[] _views = { "detail", "tile" };
        private string _workingPath = "";
        private bool _deletionSuccess = true;

        public LocalFiles()
        {
            InitializeComponent();
            _context = new FileListDataContext();
            DataContext = _context;

            var appBar = this.Resources["SelectFilesAppBar"] as ApplicationBar;
            appBar.TranslateButtons();
            ApplicationBarMenuItem selectAllMenuItem = new ApplicationBarMenuItem(Resource.Localization.AppResources.ApplicationBarMenuItem_SelectAll);
            selectAllMenuItem.Click += selectAllFiles;
            ApplicationBarMenuItem unselectAllMenuItem = new ApplicationBarMenuItem(Resource.Localization.AppResources.ApplicationBarMenuItem_UnSelectAll);
            unselectAllMenuItem.Click += unselectAllFiles;
            appBar.MenuItems.Add(selectAllMenuItem);
            appBar.MenuItems.Add(unselectAllMenuItem);
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
            FetchStructure(_workingAccount.ServerDomain + "\\" + _workingAccount.DisplayUserName);
        }

        void selectAllFiles(object sender, EventArgs e)
        {
            foreach (FileDetailViewControl detailControl in DetailList.Items)
            {
                detailControl.FileCheckbox.IsChecked = true;
            }
        }

        void unselectAllFiles(object sender, EventArgs e)
        {
            foreach (FileDetailViewControl detailControl in DetailList.Items)
            {
                detailControl.FileCheckbox.IsChecked = false;
            }
        }

        private void EnterFileSelection(object sender, EventArgs e)
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

        private void LeaveFileSelection(object sender, EventArgs e)
        {
            ApplicationBar = this.Resources["DefaultAppBar"] as ApplicationBar;
            foreach (FileDetailViewControl detailControl in DetailList.Items)
            {
                detailControl.Tap += FileItem_Tapped;
                detailControl.FileItemCheckbox.Width = new GridLength(0);
            }

        }

        private void DeleteSelectedFiles(object sender, EventArgs e)
        {
            var checkedItems = DetailList.Items.Cast<FileDetailViewControl>().Where(t => (bool)t.FileCheckbox.IsChecked);
            progress.IsVisible = true;
            progress.Text = "";
            foreach (FileDetailViewControl detailControl in checkedItems)
            {

                if ((bool)detailControl.FileCheckbox.IsChecked)
                {
                    _localStorage.DeleteFile(detailControl.FileProperties.FilePath);

                    if (App.DataContext.EnableRemoteDeleting)
                    {
                        var path = detailControl.FileProperties.FilePath.Remove(0, (_workingAccount.ServerDomain + "\\" + _workingAccount.DisplayUserName + "\\").Length).Replace("\\", "/");
                        path = "/remote.php/webdav/" + path;
                        var dav = new WebDAV(_workingAccount.GetUri(), _workingAccount.GetCredentials());
                        dav.StartRequest(DAVRequestHeader.Delete(path), checkedItems.Last() == detailControl, DeleteSelectedFilesComplete);
                    }
                }
            }
            if (!App.DataContext.EnableRemoteDeleting)
            {
                progress.IsVisible = false;
                Dispatcher.BeginInvoke(new Action(() => FetchStructure(_workingPath)));
                Dispatcher.BeginInvoke(new Action(() => LeaveFileSelection(this, null)));
            }
        }

        private void DeleteSelectedFilesComplete(DAVRequestResult result, object userObj)
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
                        Dispatcher.BeginInvoke(new Action(() => progress.IsVisible = false));
                        Dispatcher.BeginInvoke(new Action(() => FetchStructure(_workingPath)));
                        Dispatcher.BeginInvoke(new Action(() => LeaveFileSelection(this, null)));
                        break;
                    case false:
                        Dispatcher.BeginInvoke(new Action(() => progress.IsVisible = false));
                        Dispatcher.BeginInvoke(new Action(() => MessageBox.Show(Resource.Localization.AppResources.ItemDeletion_Error)));
                        Dispatcher.BeginInvoke(new Action(() => FetchStructure(_workingPath)));
                        Dispatcher.BeginInvoke(new Action(() => LeaveFileSelection(this, null)));
                        _deletionSuccess = true;
                        break;
                }
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

            BreadCrumb.Text = _workingPath.Substring(_workingAccount.ServerDomain.Length + _workingAccount.DisplayUserName.Length + 1);
        }

        private void FetchStructureComplete(IReadOnlyList<IStorageItem> items, object userObj)
        {

            bool _firstItem = false;
            // display all items linear
            // we cannot wait till an item is displayed, instead for a fluid
            // behaviour we should calculate fadeIn-delays.
            int delayStart = 0;
            int delayStep = 50; // ms

            foreach (IStorageItem item in items)
            {
                File fileItem = new File()
                {
                    FileName = item.Name,
                    FilePath = item.Path.Substring(item.Path.IndexOf("LocalState\\") + 11),
                    FileCreated = item.DateCreated.DateTime,
                    FileLastModified = item.DateCreated.DateTime,
                    IsDirectory = item.IsOfType(StorageItemTypes.Folder)
                };
                if (!fileItem.IsDirectory)
                {
                    var file = (StorageFile)item;
                    var fileInfo = new System.IO.FileInfo(file.Path);
                    fileItem.FileLastModified = fileInfo.LastWriteTime;
                    fileItem.FileSize = fileInfo.Length;
                    fileItem.FileType = file.ContentType;
                }

                bool display = true;

                Dispatcher.BeginInvoke(() =>
                {
                    if (!_firstItem)
                    {
                        _firstItem = true;

                        // Root
                        if (fileItem.IsDirectory)
                        {
                            if (!item.Path.Contains(_workingAccount.DisplayUserName))
                            {
                                // cannot go up further
                                display = false;
                            }
                            else
                            {
                                fileItem.IsRootItem = true;
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
                        detailControl.SyncButton.Visibility = System.Windows.Visibility.Collapsed;

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

        private async void StartRequest()
        {
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(_workingPath);
            IStorageItem up = await ApplicationData.Current.LocalFolder.GetItemAsync(_workingPath.Substring(0, _workingPath.LastIndexOf("\\")));

            IReadOnlyList<IStorageItem> items = await folder.GetItemsAsync();
            List<IStorageItem> itemList = items.ToList();
            itemList.Insert(0, up);
            FetchStructureComplete((IReadOnlyList<IStorageItem>)itemList, null);
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

                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(item.FilePath);
                itemControl.RotateSyncButton.Stop();
                var success = await Windows.System.Launcher.LaunchFileAsync(file);
                DetailList.SelectedIndex = -1;
            }
        }
    }
}