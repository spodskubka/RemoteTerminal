// Remote Terminal, an SSH/Telnet terminal emulator for Microsoft Windows
// Copyright (C) 2012-2015 Stefan Podskubka
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using RemoteTerminal.Common;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;
using Windows.ApplicationModel.Store;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace RemoteTerminal
{
    /// <summary>
    /// The start page displaying all favorites.
    /// </summary>
    public sealed partial class FavoritesPage : Page
    {
        /// <summary>
        /// The <see cref="NavigationHelper"/> for this page.
        /// </summary>
        private NavigationHelper navigationHelper;

        /// <summary>
        /// The default view model.
        /// </summary>
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// The current license information.
        /// </summary>
        private LicenseInformation licenseInformation;

        /// <summary>
        /// Gets the default view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> for this page.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FavoritesPage"/> class.
        /// </summary>
        public FavoritesPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.navigationHelper_LoadState;
            this.navigationHelper.SaveState += this.navigationHelper_SaveState;
            this.Loaded += this.FavoritesPage_Loaded;
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

        /// <summary>
        /// Occurs when the page has been constructed and added to the object tree, and is ready for interaction.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void FavoritesPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ChangelogManager.ShouldDisplayChangelog())
            {
                this.Frame.Navigate(typeof(ChangelogPage));
            }
        }

        /// <summary>
        /// Refreshes the display of the remaining days in trial mode at the bottom of the screen.
        /// </summary>
        /// <remarks>
        /// TODO: this is not needed anymore, the app is now free of charge.
        /// </remarks>
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

        /// <summary>
        /// Occurs when the add button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ConnectionDataForm), string.Empty);
        }

        /// <summary>
        /// Occurs when the remove button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
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

        /// <summary>
        /// Occurs when the edit button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void editButton_Click(object sender, RoutedEventArgs e)
        {
            FavoritesDataSource favoritesDataSource = (FavoritesDataSource)App.Current.Resources["favoritesDataSource"];
            if (favoritesDataSource != null)
            {
                ConnectionData selectedItem = this.itemGridView.SelectedItem as ConnectionData;
                this.Frame.Navigate(typeof(ConnectionDataForm), selectedItem.Id);
            }
        }

        /// <summary>
        /// Occurs when the quick connect button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void quickConnectButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ConnectionDataForm), null);
        }

        /// <summary>
        /// Occurs when the private keys button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void privateKeysButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(PrivateKeysPage), null);
        }

        /// <summary>
        /// Occurs when the private key agent button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void privateKeyAgentButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(PrivateKeyAgentPage), null);
        }

        /// <summary>
        /// Occurs when an item in the favorites list view is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem == null)
            {
                return;
            }

            string id = ((ConnectionData)e.ClickedItem).Id;

            var connectionData = FavoritesDataSource.GetFavorite(id);
            if (connectionData == null)
            {
                return;
            }

            Guid guid = TerminalManager.Create(connectionData);
            this.Frame.Navigate(typeof(TerminalPage), guid);
        }

        /// <summary>
        /// Occurs when the currently selected item in the favorites list view changes.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void ItemView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetupAppBar();
        }

        /// <summary>
        /// Sets up the <see cref="AppBar"/>(s).
        /// </summary>
        private void SetupAppBar()
        {
            this.BottomAppBar.IsOpen = this.itemGridView.SelectedItems.Count > 0 || this.itemGridView.Items.Count == 0;
            this.removeButton.IsEnabled = this.itemGridView.SelectedItems.Count > 0;
            this.editButton.IsEnabled = this.itemGridView.SelectedItems.Count == 1;
        }

        /// <summary>
        /// Occurs when an item in the preview grid view is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void PreviewGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            ITerminal terminal = e.ClickedItem as ITerminal;
            if (terminal == null)
            {
                return;
            }

            this.Frame.Navigate(typeof(TerminalPage), TerminalManager.GetGuid(terminal));
        }

        /// <summary>
        /// Occurs when the close button of an item in the preview grid view is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void PreviewGrid_ItemCloseButtonClick(object sender, RoutedEventArgs e)
        {
            ITerminal terminal = ((Button)sender).Tag as ITerminal;
            TerminalManager.Remove(terminal);
        }

        /// <summary>
        /// Occurs when the purchase button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        /// <remarks>
        /// TODO: this is not needed anymore, the app is now free of charge.
        /// </remarks>
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
            catch (Exception)
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
