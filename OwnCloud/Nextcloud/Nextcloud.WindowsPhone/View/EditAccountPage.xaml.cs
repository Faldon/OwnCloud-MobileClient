using Nextcloud.Common;
using Nextcloud.ViewModel;
using System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Nextcloud.Data;
using Windows.UI.Popups;
using Nextcloud.DAV;
using Windows.UI.Core;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Nextcloud.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditAccountPage : Page {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private StatusBarProgressIndicator progress;
        private CoreDispatcher dispatcher;

        public EditAccountPage() {
            this.InitializeComponent();
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            progress = StatusBar.GetForCurrentView().ProgressIndicator;
            progress.Text = App.Localization().GetString("Progress_ConnectionCheck");

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel {
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
            if(dataModel == null) {
                LayoutRoot.DataContext = new AccountViewModel(new Account());
            } else {
                LayoutRoot.DataContext = new AccountViewModel(dataModel);
            }
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

        private async void OnSaveClick(object sender, RoutedEventArgs e) {
            if ((LayoutRoot.DataContext as AccountViewModel).CanSave()) {
                dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
                AccountViewModel viewModel = LayoutRoot.DataContext as AccountViewModel;
                WebDAV client = new WebDAV(viewModel.GetWebDAVRoot(), viewModel.GetCredential());
                client.StartRequest(DAVRequestHeader.CreateListing(), DAVRequestBody.CreateAllPropertiesListing(), null, OnConnectionCheckFinished);
                await progress.ShowAsync();
            } else {
                var alert = new MessageDialog(App.Localization().GetString("EditAccountPage_SaveFailed"));
                var command = await alert.ShowAsync();
            }
            
        }

        private async void OnConnectionCheckFinished(DAVRequestResult result, object userObject)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await progress.HideAsync());
            if (!result.IsMultiState) {
                var alert = new MessageDialog(String.Format(App.Localization().GetString("EditAccountPage_ConnectionFailed"), result.Request.LastException.Message));
                alert.Commands.Add(new UICommand(App.Localization().GetString("Yes"), OnOverrideSave));
                alert.Commands.Add(new UICommand(App.Localization().GetString("No")));
                alert.CancelCommandIndex = 1;
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await alert.ShowAsync());

            } else {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SaveAccount());
            }
        }

        private async void OnOverrideSave(IUICommand command)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SaveAccount());
        }

        private async void OnCancelClick(object sender, RoutedEventArgs e) {
            if( (LayoutRoot.DataContext as AccountViewModel).CanCancel()) {
                var alert = new MessageDialog(App.Localization().GetString("EditAccountPage_CancelWarning"));
                alert.Commands.Add(new UICommand(App.Localization().GetString("Yes"), OnCancelConfirmed));
                alert.Commands.Add(new UICommand(App.Localization().GetString("No")));
                alert.CancelCommandIndex = 1;
                var command = await alert.ShowAsync();
            } else {
                var alert = new MessageDialog(App.Localization().GetString("EditAccountPage_CancelFailed"));
                alert.Commands.Add(new UICommand(App.Localization().GetString("Quit"), OnQuitConfirmed));
                alert.Commands.Add(new UICommand(App.Localization().GetString("Close")));
                alert.CancelCommandIndex = 1;
                var command = await alert.ShowAsync();
            }
        }

        private void OnCancelConfirmed(IUICommand command) {
            Frame.Navigate(typeof(AccountHubPage), null);
        }

        private void OnQuitConfirmed(IUICommand command) {
            App.Current.Exit();
        }

        private async void SaveAccount()
        {
            AccountViewModel viewModel = LayoutRoot.DataContext as AccountViewModel;
            var success = await viewModel.SaveAccount();
            Frame.Navigate(typeof(AccountHubPage), null);
        }
    }
}
