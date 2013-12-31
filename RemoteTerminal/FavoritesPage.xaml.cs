using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;
using Windows.ApplicationModel.Store;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RemoteTerminal.Common;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace RemoteTerminal
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class FavoritesPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private LicenseInformation licenseInformation;

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public FavoritesPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.navigationHelper_LoadState;
            this.navigationHelper.SaveState += this.navigationHelper_SaveState;
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            FavoritesDataSource favoritesDataSource = (FavoritesDataSource)App.Current.Resources["favoritesDataSource"];
            if (favoritesDataSource != null)
            {
                var items = new ObservableCollection<ConnectionData>(favoritesDataSource.Favorites.OrderBy(f => f.Name));
                this.DefaultViewModel["Items"] = items;

                this.emptyHint.Visibility = this.itemGridView.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                this.SetupAppBar();
            }

#if DEBUG
            this.licenseInformation = CurrentAppSimulator.LicenseInformation;
#else
            this.licenseInformation = CurrentApp.LicenseInformation;
#endif
            this.RefreshTrialHint();
            this.licenseInformation.LicenseChanged += RefreshTrialHint;

            if (TerminalManager.Terminals.Count > 0)
            {
                this.previewGrid.ItemsSource = TerminalManager.Terminals;
                this.TopAppBar.IsOpen = true;
                await Task.Delay(1000);
                this.TopAppBar.IsOpen = false;
            }
            else
            {
                this.TopAppBar = null;
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
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            this.licenseInformation.LicenseChanged -= RefreshTrialHint;
        }

        private void RefreshTrialHint()
        {
            if (this.licenseInformation.IsTrial)
            {
                this.trialPeriodDisplay.Visibility = Visibility.Visible;

                var daysRemaining = Math.Min(30, (this.licenseInformation.ExpirationDate - DateTime.Now).Days);
                this.trialPeriodDuration.Text = daysRemaining + "/30 days";
                this.trialPeriodProgressBar.Value = daysRemaining;
            }
            else
            {
                if (this.trialPeriodDisplay.Visibility == Visibility.Visible)
                {
                    purchaseThanksDisplay.Visibility = Visibility.Visible;
                }

                this.trialPeriodDisplay.Visibility = Visibility.Collapsed;
            }
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ConnectionDataForm), string.Empty);
        }

        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            FavoritesDataSource favoritesDataSource = (FavoritesDataSource)App.Current.Resources["favoritesDataSource"];
            if (favoritesDataSource != null)
            {
                var selectedItems = this.itemGridView.SelectedItems.ToArray();
                foreach (ConnectionData selectedItem in selectedItems)
                {
                    favoritesDataSource.Remove(selectedItem);
                    ((ObservableCollection<ConnectionData>)this.DefaultViewModel["Items"]).Remove(selectedItem);
                }

                this.emptyHint.Visibility = this.itemGridView.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            FavoritesDataSource favoritesDataSource = (FavoritesDataSource)App.Current.Resources["favoritesDataSource"];
            if (favoritesDataSource != null)
            {
                ConnectionData selectedItem = this.itemGridView.SelectedItem as ConnectionData;
                this.Frame.Navigate(typeof(ConnectionDataForm), selectedItem.Id);
            }
        }

        private void quickConnectButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ConnectionDataForm), null);
        }

        private void privateKeysButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(PrivateKeysPage), null);
        }

        private void privateKeyAgentButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(PrivateKeyAgentPage), null);
        }

        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem != null)
            {
                string id = ((ConnectionData)e.ClickedItem).Id;
                this.Frame.Navigate(typeof(TerminalPage), id);
            }
        }

        private void ItemView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetupAppBar();
        }

        private void SetupAppBar()
        {
            this.BottomAppBar.IsOpen = this.itemGridView.SelectedItems.Count > 0 || this.itemGridView.Items.Count == 0;
            this.removeButton.IsEnabled = this.itemGridView.SelectedItems.Count > 0;
            this.editButton.IsEnabled = this.itemGridView.SelectedItems.Count == 1;
        }

        private void PreviewGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            ITerminal terminal = e.ClickedItem as ITerminal;
            if (terminal == null)
            {
                return;
            }

            this.Frame.Navigate(typeof(TerminalPage), terminal);
        }

        private void PreviewGrid_ItemCloseButtonClick(object sender, RoutedEventArgs e)
        {
            ITerminal terminal = ((Button)sender).Tag as ITerminal;
            TerminalManager.Remove(terminal);
        }

        private async void purchaseButton_Click(object sender, RoutedEventArgs e)
        {
            bool failed = false;
            try
            {
#if DEBUG
                var s = await CurrentAppSimulator.RequestAppPurchaseAsync(false);
#else
                var s = await CurrentApp.RequestAppPurchaseAsync(false);
#endif
            }
            catch (Exception ex)
            {
                failed = true;
            }

            if (failed)
            {
                MessageDialog dialog = new MessageDialog("Purchase failed, please try again.");
                await dialog.ShowAsync();
            }
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
    }
}
