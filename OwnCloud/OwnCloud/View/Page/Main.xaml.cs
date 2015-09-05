using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using OwnCloud.Data;
using OwnCloud.Extensions;

namespace OwnCloud
{
    public partial class MainPage : PhoneApplicationPage
    {

        public MainPage()
        {
            InitializeComponent();
            DataContext = App.DataContext;
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            App.DataContext.Refresh(System.Data.Linq.RefreshMode.OverwriteCurrentValues, App.DataContext.Accounts);

            // anybody there who knows why the LINQ binding isn't working as expected?
            RemoteFiles.ItemsSource = null;
            RemoteFiles.ItemsSource = App.DataContext.Accounts;

            LocalFiles.ItemsSource = null;
            LocalFiles.ItemsSource = App.DataContext.Accounts;

            // trigger selection
            PanoramaSelectionChanged(MainPanorama, new RoutedEventArgs());

            if (App.DataContext.Accounts.Count() == 0)
            {
                var timer = new System.Threading.Timer(
                state =>
                {
                    Dispatcher.BeginInvoke(new Action(() => NavigationService.Navigate(new Uri("/View/Page/EditAccount.xaml", UriKind.Relative))));
                },
                null,
                500,
                System.Threading.Timeout.Infinite);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Reload the AccountListDataContext
            App.DataContext.Refresh(System.Data.Linq.RefreshMode.OverwriteCurrentValues, App.DataContext.Accounts);
            AccountList.DataContext = null;
            AccountList.DataContext = new AccountListDataContext();

            // update visibility of calendar panorama control
            App.DataContext.MainCalendarPageVisibility = (App.DataContext.Accounts.AsEnumerable().Any(a => a.CalendarEnabled)) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SettingsAccountsTab(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Page/AccountList.xaml", UriKind.Relative));
        }

        private void OpenCalendarTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var currentAccount = App.DataContext.Accounts.FirstOrDefault();
            if (currentAccount != null)
            {
                NavigationService.Navigate(new Uri("/View/Page/CalendarMonthPage.xaml?uid=" + String.Format(@"{0:g}", currentAccount.GUID), UriKind.Relative));
            }
        }

        private void RemoteFilesTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var currentAccount = App.DataContext.Accounts.FirstOrDefault();
            if (currentAccount != null)
            {
                NavigationService.Navigate(new Uri("/View/Page/RemoteFiles.xaml?account=" + ((Account)currentAccount).GUID, UriKind.Relative));
            }
        }

        private void LocalFilesTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var currentAccount = App.DataContext.Accounts.FirstOrDefault();
            if (currentAccount != null)
            {
                NavigationService.Navigate(new Uri("/View/Page/LocalFiles.xaml?account=" + ((Account)currentAccount).GUID, UriKind.Relative));
            }
        }

        private void EditAccountTap(object sender, EventArgs e)
        {
            var currentAccount = App.DataContext.Accounts.First();
            NavigationService.Navigate(new Uri("/View/Page/EditAccount.xaml?mode=edit&account=" + ((Account)currentAccount).GUID, UriKind.Relative));
        }

        private void CalendarPinToStart(object sender, RoutedEventArgs e)
        {
            var accountID = ((sender as FrameworkElement).DataContext as Account).GUID;

            Extensions.TileHelper.AddCalendarToTile(accountID);
        }

        private void RemoteFilesPinToStart(object sender, RoutedEventArgs e)
        {
            var accountID = ((sender as FrameworkElement).DataContext as Account).GUID;

            Extensions.TileHelper.AddOnlineFilesToTile(accountID);
        }

        private void LocaFilesPinToStart(object sender, RoutedEventArgs e)
        {
            var accountID = ((sender as FrameworkElement).DataContext as Account).GUID;

            Extensions.TileHelper.AddLocalFilesToTile(accountID);
        }

        private void PanoramaSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ApplicationBar != null) ApplicationBar.IsVisible = false;

            var panoramaItem = (PanoramaItem)(sender as Panorama).SelectedItem;
            switch (panoramaItem.Name)
            {
                case "AccountPanoramaItem":
                    ApplicationBar = (ApplicationBar)Resources["AccountApplicationBar"];
                    ApplicationBar.IsVisible = true;
                    ApplicationBar.TranslateButtons();
                    break;
                case "FilesPanoramaItem":
                    ApplicationBar = (ApplicationBar)Resources["FilesApplicationBar"];
                    ApplicationBar.IsVisible = true;
                    ApplicationBar.TranslateButtons();
                    break;
            }
        }

        private void AddAccountTap(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Page/EditAccount.xaml", UriKind.Relative));
        }

        private void ChooseAccountTap(object sender, EventArgs e)
        {
            // dynamicly create menu list
            var button = sender as ApplicationBarIconButton;
            ApplicationBar.IsMenuEnabled = true;
            ApplicationBar.MenuItems.Clear();

            foreach (var acc in App.DataContext.Accounts)
            {
                var item = new ApplicationBarMenuItem(acc.ServerDomain);
                ApplicationBar.MenuItems.Add(item);
            }
        }

        private void PanoramaManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {

        }
    }
}