using Nextcloud.Common;
using Nextcloud.Data;
using Nextcloud.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Nextcloud.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FileListPage : Page {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private static StatusBarProgressIndicator progress;
        private static CoreDispatcher dispatcher;

        public static readonly DependencyProperty IsProgressVisibleProperty = DependencyProperty.Register("IsFetching", typeof(bool), typeof(FileListPage), new PropertyMetadata(false, OnIsFetchingChanged));
        public static readonly DependencyProperty ErrorMessageProperty = DependencyProperty.Register("LastError", typeof(string), typeof(FileListPage), new PropertyMetadata("", OnErrorChanged));

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

        private bool _isSelectView;

        public FileListPage() {
            this.InitializeComponent();
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            _isSelectView = false;

            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            progress = StatusBar.GetForCurrentView().ProgressIndicator;
            progress.Text = App.Localization().GetString("Progress_FetchingStructure");

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

        private void FetchStructure(string path) {
            (LayoutRoot.DataContext as FileListViewModel).StartFetching(path);
        }

       private void ToggleSecondaryCommands(bool isVisible) {
            if (isVisible) {
                foreach (ICommandBarElement command in BottomAppBar.SecondaryCommands.ToList()) {
                    (command as AppBarButton).Visibility = Visibility.Visible;
                }
            } else {
                foreach (ICommandBarElement command in BottomAppBar.SecondaryCommands.ToList()) {
                    (command as AppBarButton).Visibility = Visibility.Collapsed;
                }
            }
        }

        public void Continue(IContinuationActivatedEventArgs args) {
            if(args.Kind == ActivationKind.PickFileContinuation) {
                var openPickerContinuationArgs = args as FileOpenPickerContinuationEventArgs;
                FileListViewModel viewModel = LayoutRoot.DataContext as FileListViewModel;
                viewModel.CurrentPath = openPickerContinuationArgs.ContinuationData["UploadPath"] as string;
                if ((openPickerContinuationArgs.ContinuationData["Operation"] as string) == "Fileupload" && openPickerContinuationArgs.Files != null && openPickerContinuationArgs.Files.Count > 0) {
                    progress.Text = App.Localization().GetString("Progress_UploadingFiles");
                    viewModel.UploadFilesAsync(openPickerContinuationArgs.Files.ToList());
                }
            }
        }

        #region event handling
        private static async void OnIsFetchingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if ((bool)e.NewValue) {
                await progress.ShowAsync();
            } else {
                await progress.HideAsync();
            }
        }

        private static async void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if ((e.NewValue as string).Length > 0) {
                var alert = new MessageDialog(String.Format(App.Localization().GetString("FileListPage_FetchingFailed"), (string)e.NewValue));
                alert.CancelCommandIndex = 1;
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await alert.ShowAsync());
            }
        }

        private async void OnFileItemTapped(object sender, TappedRoutedEventArgs e) {
            if (_isSelectView) {
                return;
            }
            File tappedItem = (File)(sender as FrameworkElement).DataContext;
            if (tappedItem.IsDirectory) {
                progress.Text = App.Localization().GetString("Progress_FetchingStructure");
                FetchStructure(tappedItem.Filepath.Remove(0, tappedItem.Account.Server.WebDAVPath.Length - 1));
            } else {
                Uri uriToDownload = new Uri(tappedItem.Account.Server.Protocol + "://" + tappedItem.Account.Server.FQDN + tappedItem.Filepath, UriKind.Absolute);
                progress.Text = App.Localization().GetString("Progress_FetchingFiles");
                bool result = await (LayoutRoot.DataContext as FileListViewModel).DownloadFileAsync(uriToDownload, tappedItem.Filename, tappedItem.FileLastModified, new CancellationToken(false));
                if (result) {
                    tappedItem.IsDownloaded = true;
                    App.GetDataContext().UpdateFileAsync(tappedItem);
                    tappedItem.NotifyPropertyChanged("IsDownloaded");
                }
            }
        }

        private void OnUploadClick(object sender, RoutedEventArgs e) {
            FileOpenPicker opener = new FileOpenPicker();
            opener.ViewMode = PickerViewMode.Thumbnail;
            opener.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            opener.FileTypeFilter.Add("*");
            opener.ContinuationData["Operation"] = "Fileupload";
            opener.ContinuationData["UploadPath"] = CurrentPath.Text;
            opener.PickMultipleFilesAndContinue();
        }

        private async void OnMakeCollectionClick(object sender, RoutedEventArgs e) {
            var enterNewFolderName = new ContentDialog() {
                Title = App.Localization().GetString("Dialog_NewFolderTitle"),
                MaxWidth = this.ActualWidth
            };
            var dialogPanel = new StackPanel();
            dialogPanel.Children.Add(new TextBlock {
                Text = App.Localization().GetString("Dialog_NewFolderContent")
            });
            dialogPanel.Children.Add(new TextBox {
                Name = "NewFolderName"
            });
            enterNewFolderName.Content = dialogPanel;
            enterNewFolderName.PrimaryButtonText = App.Localization().GetString("OK");
            enterNewFolderName.PrimaryButtonClick += OnEnterNewFolderNameConfirmed;
            
            var result = await enterNewFolderName.ShowAsync();
        }

        private async void OnEnterNewFolderNameConfirmed(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            StackPanel dialogContent = (sender.Content as StackPanel);
            TextBox input = dialogContent.Children.Where(e => e.GetType() == typeof(TextBox)).FirstOrDefault() as TextBox;
            string folderName = input.Text;
            if(folderName == "") {
                var alert = new MessageDialog(App.Localization().GetString("FileListPage_EmptyFolderName"));
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await alert.ShowAsync());
            } else {
                progress.Text = App.Localization().GetString("Progress_CreatingFolder");
                FileListViewModel viewModel = LayoutRoot.DataContext as FileListViewModel;
                viewModel.CreateFolderAsync(folderName);
            }
        }

        private void OnSelectClick(object sender, RoutedEventArgs e) {
            if(_isSelectView) {
                FileListView.SelectionMode = ListViewSelectionMode.None;
            } else {
                FileListView.SelectionMode = ListViewSelectionMode.Multiple;
                FileListView.IsItemClickEnabled = false;
            }
            ToggleSecondaryCommands(!_isSelectView);
            _isSelectView = !_isSelectView;
        }

        private void OnSelectAllClick(object sender, RoutedEventArgs e) {
            FileListView.SelectAll();
        }

        private void OnDeleteLocalClick(object sender, RoutedEventArgs e) {
            progress.Text = App.Localization().GetString("Progress_DeletingFiles");
            List<object> selectedItems = FileListView.SelectedItems.ToList();
            foreach(File fileToDelete in selectedItems) {
                if(!fileToDelete.IsRootItem) {
                    (LayoutRoot.DataContext as FileListViewModel).DeleteFileAsync(fileToDelete);
                }
            }
            OnSelectClick(null, null);
        }

        private void OnDeleteBothClick(object sender, RoutedEventArgs e) {
            progress.Text = App.Localization().GetString("Progress_DeletingFiles");
            List<object> selectedItems = FileListView.SelectedItems.ToList();
            foreach (File fileToDelete in selectedItems) {
                if(!fileToDelete.IsRootItem) {
                    (LayoutRoot.DataContext as FileListViewModel).DeleteFileAsync(fileToDelete, remote: true);
                }
            }
            OnSelectClick(null, null);
        }
        #endregion
    }
}
