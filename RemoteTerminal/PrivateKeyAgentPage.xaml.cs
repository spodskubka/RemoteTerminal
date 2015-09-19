using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using RemoteTerminal.Common;
using RemoteTerminal.Model;
using Renci.SshNet;
using Renci.SshNet.Common;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace RemoteTerminal
{
    /// <summary>
    /// The page to load/unload the keys in the <see cref="PrivateKeyAgent"/> singleton.
    /// </summary>
    /// <remarks>
    /// The <see cref="PrivateKeyAgent"/> singleton is accessed through the <see cref="PrivateKeyAgentManager"/> class.
    /// </remarks>
    public sealed partial class PrivateKeyAgentPage : Page
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
        /// Initializes a new instance of the <see cref="PrivateKeyAgentPage"/> class.
        /// </summary>
        public PrivateKeyAgentPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.navigationHelper_LoadState;
        }

        /// <summary>
        /// Gets or sets an observable collection of keys in the <see cref="PrivateKeyAgent"/>.
        /// </summary>
        private ObservableCollection<PrivateKeyAgentKey> AgentKeys { get; set; }

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
            // TODO: Assign a bindable collection of items to this.DefaultViewModel["Items"]
            PrivateKeysDataSource privateKeysDataSource = (PrivateKeysDataSource)App.Current.Resources["privateKeysDataSource"];
            if (privateKeysDataSource != null)
            {
                var keys = new ObservableCollection<PrivateKeyData>(privateKeysDataSource.PrivateKeys.OrderBy(f => f.FileName));
                this.DefaultViewModel["Keys"] = keys;
            }

            this.AgentKeys = new ObservableCollection<PrivateKeyAgentKey>(PrivateKeyAgentManager.PrivateKeyAgent.ListSsh2());
            this.DefaultViewModel["AgentKeys"] = this.AgentKeys;

            this.SetEmptyHintVisibilities();
        }

        /// <summary>
        /// Shows/hides the "empty hints" of the available and loaded key lists.
        /// </summary>
        private void SetEmptyHintVisibilities()
        {
            Visibility keysEmptyVisibility = this.keysGridView.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            this.keysGridEmptyHint.Visibility = keysEmptyVisibility;

            Visibility agentKeysEmptyVisibility = this.agentKeysGridView.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            this.agentKeysGridEmptyHint.Visibility = agentKeysEmptyVisibility;
        }

        /// <summary>
        /// Occurs when an item in the list of available keys is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private async void Keys_ItemClick(object sender, ItemClickEventArgs e)
        {
            PrivateKeyData privateKeyData = e.ClickedItem as PrivateKeyData;
            if (e.ClickedItem == null)
            {
                return;
            }

            MessageDialog dlg = null;
            try
            {
                PrivateKeyFile privateKey;
                using (var privateKeyStream = new MemoryStream(privateKeyData.Data))
                {
                    privateKey = new PrivateKeyFile(privateKeyStream);
                }

                var addedAgentKey = PrivateKeyAgentManager.PrivateKeyAgent.AddSsh2(privateKey.HostKey, privateKeyData.FileName);
                if (addedAgentKey != null)
                {
                    this.AgentKeys.Add(addedAgentKey);
                }
                else
                {
                    dlg = new MessageDialog("This private key is already loaded into the agent.", "Error loading private key");
                }
                this.SetEmptyHintVisibilities();
            }
            catch (SshPassPhraseNullOrEmptyException)
            {
                var clickedItem = ((ListViewBase)sender).ContainerFromItem(e.ClickedItem);
                this.loadKeyPasswordErrorTextBlock.Visibility = Visibility.Collapsed;
                this.loadKeyPasswordBox.Tag = e.ClickedItem;
                Flyout.GetAttachedFlyout((ListViewBase)this.keysGridView).ShowAt((FrameworkElement)clickedItem);
                this.loadKeyPasswordBox.Focus(FocusState.Programmatic);
            }
            catch (SshException ex)
            {
                dlg = new MessageDialog(ex.Message, "Error loading private key");
            }

            if (dlg != null)
            {
                await dlg.ShowAsync();
            }
        }

        /// <summary>
        /// Occurs when the load key button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private async void keyLoadButton_Click(object sender, RoutedEventArgs e)
        {
            PrivateKeyData privateKeyData = this.loadKeyPasswordBox.Tag as PrivateKeyData;
            if (privateKeyData == null)
            {
                return;
            }

            try
            {
                PrivateKeyFile privateKey;
                using (var privateKeyStream = new MemoryStream(privateKeyData.Data))
                {
                    privateKey = new PrivateKeyFile(privateKeyStream, loadKeyPasswordBox.Password);
                }

                var addedAgentKey = PrivateKeyAgentManager.PrivateKeyAgent.AddSsh2(privateKey.HostKey, privateKeyData.FileName);
                if (addedAgentKey != null)
                {
                    this.AgentKeys.Add(addedAgentKey);
                }
                else
                {
                    MessageDialog dlg = new MessageDialog("This private key is already loaded into the private key agent.", "Error loading private key");
                    await dlg.ShowAsync();
                }

                Flyout.GetAttachedFlyout(this.keysGridView).Hide();
                this.SetEmptyHintVisibilities();
            }
            catch (Exception ex)
            {
                this.loadKeyPasswordErrorTextBlock.Text = "Wrong password.";
                this.loadKeyPasswordErrorTextBlock.Visibility = Visibility.Visible;
                this.loadKeyPasswordBox.Focus(FocusState.Programmatic);
            }

            this.loadKeyPasswordBox.Password = string.Empty;
        }

        /// <summary>
        /// Occurs when an item in the list of loaded keys is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void AgentKeys_ItemClick(object sender, ItemClickEventArgs e)
        {
            PrivateKeyAgentKey agentKey = e.ClickedItem as PrivateKeyAgentKey;
            if (agentKey == null)
            {
                return;
            }

            var clickedItem = ((ListViewBase)sender).ContainerFromItem(e.ClickedItem);
            MenuFlyout menuFlyout = new MenuFlyout()
            {
                Placement = FlyoutPlacementMode.Bottom,
            };

            MenuFlyoutItem menuItemUnload = new MenuFlyoutItem();
            menuItemUnload.Text = "Unload";
            menuItemUnload.Tapped += (a, b) =>
            {
                PrivateKeyAgentManager.PrivateKeyAgent.RemoveSsh2(agentKey.Key.Data);
                this.AgentKeys.Remove(agentKey);
                this.SetEmptyHintVisibilities();
            };

            //MenuFlyoutItem menuItemImport = new MenuFlyoutItem();
            //menuItemImport.Text = "Import";
            //menuItemImport.Tapped += (a, b) =>
            //{
            //    PrivateKeysDataSource privateKeysDataSource = (PrivateKeysDataSource)App.Current.Resources["privateKeysDataSource"];
            //    if (privateKeysDataSource != null)
            //    {
            //        var privateKeysFolder = await PrivateKeysDataSource.GetPrivateKeysFolder();

            //        char[] fileNameChars = agentKey.Comment.ToCharArray();
            //        char[] invalidChars = Path.GetInvalidFileNameChars();
            //        for (int i = 0; i < fileNameChars.Length; i++)
            //        {
            //            if (invalidChars.Contains(fileNameChars[i]))
            //            {
            //                fileNameChars[i] = '_';
            //            }
            //        }

            //        string fileName = new string(fileNameChars);
            //        agentKey.Key.Key.

            //        var privateKeyFile = await file.CopyAsync(privateKeysFolder, file.Name, NameCollisionOption.GenerateUniqueName);

            //        var privateKeyData = new PrivateKeyData()
            //        {
            //            FileName = privateKeyFile.Name,
            //            Data = (await FileIO.ReadBufferAsync(privateKeyFile)).ToArray(),
            //        };

            //        privateKeysDataSource.PrivateKeys.Remove(PrivateKeysDataSource.GetPrivateKey(privateKeyData.FileName));
            //        privateKeysDataSource.PrivateKeys.Add(privateKeyData);

            //        this.SetEmptyHintVisibilities();
            //    }
            //};

            menuFlyout.Items.Add(menuItemUnload);
            //menu.Items.Add(menuItemImport);

            menuFlyout.ShowAt((FrameworkElement)clickedItem);

            //PopupMenu menu = new PopupMenu();
            //menu.Commands.Add(new UICommand("Unload"));
            //menu.Commands.Add(new UICommand("Save locally"));
            //var command = await menu.ShowAsync(new Point(0, 0));
            //if (command == null)
            //{
            //    return;
            //}
        }

        /// <summary>
        /// Occurs when a keyboard key is pressed while the load key password box has focus.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void loadKeyPasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                this.keyLoadButton_Click(sender, e);
                e.Handled = true;
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
