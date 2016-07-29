using Nextcloud.Common;
using Nextcloud.Data;
using Nextcloud.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Nextcloud.View {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FileListPage : Page {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private static StatusBarProgressIndicator progress;
        private static CoreDispatcher dispatcher;

        public static readonly DependencyProperty IsProgressVisibleProperty = DependencyProperty.Register("IsFetching", typeof(bool), typeof(FileListPage), new PropertyMetadata(false, OnIsFetchingChanged));
        public static readonly DependencyProperty ErrorMessageProperty = DependencyProperty.Register("Error", typeof(string), typeof(FileListPage), new PropertyMetadata("", OnErrorChanged));

        public bool IsProgressVisible
        {
            get { return (bool)GetValue(IsProgressVisibleProperty); }
            set { SetValue(IsProgressVisibleProperty, value); }
        }

        public string ErrorMessage
        {
            get { return (string)GetValue(ErrorMessageProperty); }
            set { SetValue(ErrorMessageProperty, value); }
        }

        private static async void OnIsFetchingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if((bool)e.NewValue) {
                await progress.ShowAsync();
            } else {
                await progress.HideAsync();
            }
        }

        private static async void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if((e.NewValue as string).Length > 0) {
                var alert = new MessageDialog(String.Format(App.Localization().GetString("FileListPage_FetchingFailed"), (string)e.NewValue));
                alert.CancelCommandIndex = 1;
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await alert.ShowAsync());
            }
        }

        public FileListPage() {
            this.InitializeComponent();
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            progress = StatusBar.GetForCurrentView().ProgressIndicator;
            progress.Text = App.Localization().GetString("Progress_FetchingFiles");

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e) {
            Account dataModel = e.NavigationParameter as Account;
            LayoutRoot.DataContext = new FileListViewModel(dataModel);

            var progressBinding = new Binding {
                Path = new PropertyPath("IsFetching"),
                Source = LayoutRoot.DataContext,
                RelativeSource = new RelativeSource {
                    Mode = RelativeSourceMode.Self
                }
            };
            SetBinding(IsProgressVisibleProperty, progressBinding);

            var errorBinding = new Binding {
                Path = new PropertyPath("LastError"),
                Source = LayoutRoot.DataContext,
                RelativeSource = new RelativeSource {
                    Mode = RelativeSourceMode.Self
                }
            };
            SetBinding(ErrorMessageProperty, errorBinding);

            (LayoutRoot.DataContext as FileListViewModel).StartFetching();
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e) {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void OnFileItemTapped(object sender, TappedRoutedEventArgs e) {
            File tappedItem = (File)(sender as FrameworkElement).DataContext;
            if(tappedItem.IsDirectory) {
                FetchStructure(tappedItem.Filepath.Remove(0, tappedItem.Account.Server.WebDAVPath.Length-1));
            } else {
                Uri uriToDownload = new Uri(tappedItem.Account.Server.Protocol + "://" + tappedItem.Account.Server.FQDN + tappedItem.Filepath, UriKind.Absolute);
                bool result = await DownloadFileAsync(uriToDownload, tappedItem.Filename, tappedItem.FileLastModified, new CancellationToken(false));
            }
        }

        private void FetchStructure(string path) {
            (LayoutRoot.DataContext as FileListViewModel).StartFetching(path);
        }

        private async Task<bool> DownloadFileAsync(Uri uriToDownload, string fileName, DateTime lastModificationDate, CancellationToken cToken) {
            try {
                FileListViewModel viewModel = LayoutRoot.DataContext as FileListViewModel;
                //var localPath = _workingAccount.ServerDomain + "\\" + _workingAccount.DisplayUserName + "\\" + _workingPath.Remove(0, _workingAccount.WebDAVPath.Length).Replace("/", "\\");
                // var userFile = localPath + fileName;
                StorageFolder localStorage = ApplicationData.Current.LocalFolder;
                StorageFolder server = await localStorage.CreateFolderAsync(viewModel.Servername, CreationCollisionOption.OpenIfExists);
                StorageFolder user = await server.CreateFolderAsync(viewModel.Username, CreationCollisionOption.OpenIfExists);
                StorageFolder cwd = user;

                foreach (string folder in viewModel.CurrentPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)) {
                    cwd = await cwd.CreateFolderAsync(folder, CreationCollisionOption.OpenIfExists);
                }

                StorageFile localFile;
                try {
                    localFile = await cwd.GetFileAsync(fileName);
                    if (localFile.DateCreated > lastModificationDate) {
                        return true;
                    }
                } catch (Exception e) {
                    localFile = await cwd.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
                }
                BackgroundDownloader loader = new BackgroundDownloader();
                var cred = await viewModel.Account.GetCredential();
                loader.ServerCredential = new Windows.Security.Credentials.PasswordCredential(viewModel.Servername, cred.UserName, cred.Password);
                DownloadOperation dl = loader.CreateDownload(uriToDownload, localFile);
                var dloperation = await Task.Run(() => { return dl.StartAsync(); });
                dloperation.Completed = delegate (IAsyncOperationWithProgress<DownloadOperation, DownloadOperation> dlCompleted) {

                };
                //dloperation.Completed += new AsyncOperationWithProgressCompletedHandler<DownloadOperation, DownloadOperation>(IAsyncOperationWithProgress<DownloadOperation, DownloadOperation>, dloperation)

                return true;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }


        //private static Task<System.IO.Stream> DownloadFile(Uri url, NetworkCredential credentials) {
        //    var tcs = new TaskCompletionSource<System.IO.Stream>();
        //    var wc = new HttpClient();
            
        //    wc.Credentials = credentials;
        //    wc.OpenReadCompleted += (s, e) => {
        //        if (e.Error != null) tcs.TrySetException(e.Error);
        //        else if (e.Cancelled) tcs.TrySetCanceled();
        //        else tcs.TrySetResult(e.Result);
        //    };
        //    wc.OpenReadAsync(url);
        //    return tcs.Task;
        //}
    }
}
