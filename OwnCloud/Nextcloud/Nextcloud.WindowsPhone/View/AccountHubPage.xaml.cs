using Nextcloud.Common;
using Nextcloud.Data;
using Nextcloud.ViewModel;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using SQLite.Net;
using SQLiteNetExtensions.Extensions;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Popups;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace Nextcloud.View
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class AccountHubPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        public AccountHubPage()
        {
            this.InitializeComponent();

            // Hub is only supported in Portrait orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            this.NavigationCacheMode = NavigationCacheMode.Required;

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
        /// <see cref="Frame.Navigate(Type, object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            List<Account> dataModel = e.NavigationParameter as List<Account>;
            if (dataModel == null) {
                dataModel = App.GetDataContext().GetConnection().GetAllWithChildren<Account>(recursive:true);
                LayoutRoot.DataContext = new AccountHubViewModel(dataModel);
            } else {
                LayoutRoot.DataContext = new AccountHubViewModel(dataModel);
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
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
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
        /// <param name="e">Event data that describes how this page was reached.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }
        #endregion

        private void OnAccountItemHold(object sender, HoldingRoutedEventArgs e) {
            FrameworkElement s = sender as FrameworkElement;
            FlyoutBase.GetAttachedFlyout(s).ShowAt(s);
        }

        private void OnMenuEditClick(object sender, RoutedEventArgs e) {
            Account selectedAccount = (Account)(sender as MenuFlyoutItem).DataContext;
            Frame.Navigate(typeof(EditAccountPage), selectedAccount);
        }

        private async void OnMenuDeleteClick(object sender, RoutedEventArgs e) {
            Account selectedAccount = (Account)(sender as MenuFlyoutItem).DataContext;
            if ((LayoutRoot.DataContext as AccountHubViewModel).CanDelete(selectedAccount)) {
                var alert = new MessageDialog(String.Format(App.Localization().GetString("AccountHubPage_DeleteAccountWarning"), selectedAccount.Username));
                alert.Commands.Add(new UICommand(App.Localization().GetString("Yes")));
                alert.Commands.Add(new UICommand(App.Localization().GetString("No")));
                alert.CancelCommandIndex = 1;
                var command = await alert.ShowAsync();
                if (command.Label.Equals(App.Localization().GetString("Yes"))) {
                    AccountHubViewModel viewModel = LayoutRoot.DataContext as AccountHubViewModel;
                    viewModel.DeleteAccount(selectedAccount);
                }
            } else {
                var alert = new MessageDialog(App.Localization().GetString("AccountHubPage_DeleteAccountFailed"));
                var command = await alert.ShowAsync();
            }
        }

        private void OnAddAccountClick(object sender, RoutedEventArgs e) {
            Frame.Navigate(typeof(EditAccountPage), null);
        }
    }
}