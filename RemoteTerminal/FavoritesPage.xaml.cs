using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace RemoteTerminal
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class FavoritesPage : RemoteTerminal.Common.LayoutAwarePage
    {
        public FavoritesPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            FavoritesDataSource favoritesDataSource = (FavoritesDataSource)App.Current.Resources["favoritesDataSource"];
            if (favoritesDataSource != null)
            {
                var items = new ObservableCollection<ConnectionData>(favoritesDataSource.Favorites.OrderBy(f => f.Name));
                this.DefaultViewModel["Items"] = items;

                this.emptyHint.Visibility = this.itemGridView.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                this.SetupAppBar();
            }

            this.previewGrid.ItemsSource = TerminalManager.Terminals;
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

                this.emptyHint.Visibility = this.itemGridView.Items.Count == 0 ? Visibility.Visible:Visibility.Collapsed;
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
            ListViewBase otherList;

            if (sender == this.itemGridView)
            {
                otherList = this.itemListView;
            }
            else
            {
                otherList = this.itemGridView;
            }

            foreach (var added in e.AddedItems)
            {
                if (!otherList.SelectedItems.Contains(added))
                {
                    otherList.SelectedItems.Add(added);
                }
            }

            foreach (var removed in e.RemovedItems)
            {
                if (otherList.SelectedItems.Contains(removed))
                {
                    otherList.SelectedItems.Remove(removed);
                }
            }

            SetupAppBar();
        }

        private void SetupAppBar()
        {
            this.bottomAppBar.IsOpen = this.itemGridView.SelectedItems.Count > 0 || this.itemGridView.Items.Count == 0;
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
    }
}
