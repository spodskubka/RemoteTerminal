using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using RemoteTerminal.Common;
using RemoteTerminal.Model;
using Renci.SshNet;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace RemoteTerminal
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class PrivateKeysPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

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

        public PrivateKeysPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.navigationHelper_LoadState;
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
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
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

        private async void deleteButton_Click(object sender, RoutedEventArgs e)
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
            SetupAppBar();
        }

        private void SetupAppBar()
        {
            this.BottomAppBar.IsOpen = this.itemGridView.SelectedItems.Count > 0 || this.itemGridView.Items.Count == 0;
            this.deleteButton.IsEnabled = this.itemGridView.SelectedItems.Count > 0;
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
