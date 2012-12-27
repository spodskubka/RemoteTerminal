using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using RemoteTerminal.Model;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Runtime.InteropServices.WindowsRuntime;
using Renci.SshNet;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace RemoteTerminal
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class PrivateKeysPage : RemoteTerminal.Common.LayoutAwarePage
    {
        public PrivateKeysPage()
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
            PrivateKeysDataSource privateKeysDataSource = (PrivateKeysDataSource)App.Current.Resources["privateKeysDataSource"];
            if (privateKeysDataSource != null)
            {
                var items = new ObservableCollection<PrivateKeyData>(privateKeysDataSource.PrivateKeys.OrderBy(f => f.FileName));
                this.DefaultViewModel["Items"] = items;

                this.emptyHint.Visibility = this.itemGridView.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                this.SetupAppBar();
            }
        }

        private async void importButton_Click(object sender, RoutedEventArgs e)
        {
            // File picker APIs don't work if the app is in a snapped state.
            // If the app is snapped, try to unsnap it first. Only show the picker if it unsnaps.
            if (ApplicationView.Value == ApplicationViewState.Snapped && ApplicationView.TryUnsnap() == false)
            {
                return;
            }

            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.ViewMode = PickerViewMode.List;
            openPicker.CommitButtonText = "Import";

            // Filter to include a sample subset of file types.
            openPicker.FileTypeFilter.Clear();
            //openPicker.FileTypeFilter.Add(".pfx");
            openPicker.FileTypeFilter.Add(".pem");
            openPicker.FileTypeFilter.Add(".key");
            openPicker.FileTypeFilter.Add(".ssh");

            // Open the file picker.
            var files = await openPicker.PickMultipleFilesAsync();

            // files is null if user cancels the file picker.
            if (files == null || files.Count == 0)
            {
                return;
            }

            var privateKeysDataSource = App.Current.Resources["privateKeysDataSource"] as PrivateKeysDataSource;
            if (privateKeysDataSource == null)
            {
                return;
            }

            foreach (var file in files)
            {
                var buffer = await FileIO.ReadBufferAsync(file);
                using (var stream = new MemoryStream(buffer.ToArray()))
                {
                    PrivateKeyFile.Validate(stream);
                }

                var privateKeysFolder = await PrivateKeysDataSource.GetPrivateKeysFolder();
                var privateKeyFile = await file.CopyAsync(privateKeysFolder, file.Name, NameCollisionOption.GenerateUniqueName);

                var privateKeyData = new PrivateKeyData()
                {
                    FileName = privateKeyFile.Name,
                    Data = (await FileIO.ReadBufferAsync(privateKeyFile)).ToArray(),
                };

                privateKeysDataSource.PrivateKeys.Remove(PrivateKeysDataSource.GetPrivateKey(privateKeyData.FileName));
                privateKeysDataSource.PrivateKeys.Add(privateKeyData);
            }

            var items = new ObservableCollection<PrivateKeyData>(privateKeysDataSource.PrivateKeys.OrderBy(f => f.FileName));
            this.DefaultViewModel["Items"] = items;
            this.emptyHint.Visibility = this.itemGridView.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void discardButton_Click(object sender, RoutedEventArgs e)
        {
            PrivateKeysDataSource privateKeysDataSource = (PrivateKeysDataSource)App.Current.Resources["privateKeysDataSource"];
            if (privateKeysDataSource != null)
            {
                var selectedItems = this.itemGridView.SelectedItems.ToArray();
                foreach (PrivateKeyData selectedItem in selectedItems)
                {
                    await privateKeysDataSource.Remove(selectedItem);
                    ((ObservableCollection<PrivateKeyData>)this.DefaultViewModel["Items"]).Remove(selectedItem);
                }

                this.emptyHint.Visibility = this.itemGridView.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
            this.discardButton.IsEnabled = this.itemGridView.SelectedItems.Count > 0;
        }
    }
}
