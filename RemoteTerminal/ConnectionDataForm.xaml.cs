using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RemoteTerminal.Model;
using Renci.SshNet;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RemoteTerminal.Common;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace RemoteTerminal
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class ConnectionDataForm : Page
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

        private const int DefaultSshPort = 22;
        private const int DefaultTelnetPort = 23;

        enum ConnectionDataMode
        {
            QuickConnect,
            New,
            Edit,
        }

        private string id;
        private ConnectionDataMode mode;

        public ConnectionDataForm()
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
            this.mode = ConnectionDataMode.Edit;

            string id = e.NavigationParameter as string;

            this.sshRadioButton.IsChecked = true;
            this.authenticationMethodComboBox.SelectedIndex = 0;
            PrivateKeysDataSource privateKeysDataSource = (PrivateKeysDataSource)App.Current.Resources["privateKeysDataSource"];
            if (privateKeysDataSource != null)
            {
                var privateKeys = from privateKey in privateKeysDataSource.PrivateKeys
                                  orderby privateKey.FileName
                                  select privateKey.FileName;
                this.privateKeyComboBox.ItemsSource = privateKeys;
            }

            if (id == null)
            {
                this.mode = ConnectionDataMode.QuickConnect;
                this.nameOptions.Visibility = Visibility.Collapsed;
            }
            else if (id.Length == 0)
            {
                this.mode = ConnectionDataMode.New;
            }
            else
            {
                ConnectionData connectionData = FavoritesDataSource.GetFavorite(id);
                if (connectionData == null)
                {
                    this.Frame.GoBack();
                    return;
                }

                this.nameTextBox.Text = connectionData.Name;
                switch (connectionData.Type)
                {
                    case ConnectionType.Ssh:
                        this.sshRadioButton.IsChecked = true;
                        this.telnetRadioButton.IsChecked = false;
                        break;
                    case ConnectionType.Telnet:
                        this.sshRadioButton.IsChecked = false;
                        this.telnetRadioButton.IsChecked = true;
                        break;
                }
                this.hostTextBox.Text = connectionData.Host;
                this.portTextBox.Text = connectionData.Port.ToString();
                this.usernameTextBox.Text = connectionData.Username;
                this.authenticationMethodComboBox.SelectedIndex = (int)connectionData.Authentication;
                this.privateKeyComboBox.SelectedItem = connectionData.PrivateKeyName;
                this.privateKeyAgentForwardingCheckBox.IsChecked = connectionData.PrivateKeyAgentForwarding;
                this.id = connectionData.Id;
            }

            this.SetupPageTitle();
            this.SetupAppBar();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            FavoritesDataSource favoritesDataSource = (FavoritesDataSource)App.Current.Resources["favoritesDataSource"];
            if (favoritesDataSource != null)
            {
                var connectionData = this.CreateConnectionDataFromForm();

                favoritesDataSource.AddOrUpdate(connectionData);
            }

            this.Frame.GoBack();
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            var connectionData = this.CreateConnectionDataFromForm();

            Guid guid = TerminalManager.Create(connectionData);
            this.Frame.Navigate(typeof(TerminalPage), guid);
        }

        private ConnectionData CreateConnectionDataFromForm()
        {
            string name = string.Empty;
            if (this.mode == ConnectionDataMode.QuickConnect)
            {
                if (this.usernameTextBox.Text.Length > 0)
                {
                    name = this.usernameTextBox.Text + "@";
                }

                name += this.hostTextBox.Text + ":" + this.portTextBox.Text;
            }
            else
            {
                name = this.nameTextBox.Text;
            }

            return new ConnectionData()
            {
                Id = this.id,
                Name = name,
                Type = (this.sshRadioButton.IsChecked ?? false) ? ConnectionType.Ssh : ConnectionType.Telnet,
                Host = this.hostTextBox.Text,
                Port = int.Parse(this.portTextBox.Text),
                Username = this.usernameTextBox.Text,
                Authentication = (AuthenticationType)this.authenticationMethodComboBox.SelectedIndex,
                PrivateKeyName = this.privateKeyComboBox.SelectedItem as string ?? string.Empty,
                PrivateKeyAgentForwarding = this.privateKeyAgentForwardingCheckBox.IsChecked ?? false,
            };
        }

        private void SetupAppBar()
        {
            this.connectButton.Visibility = this.mode == ConnectionDataMode.QuickConnect ? Visibility.Visible : Visibility.Collapsed;
            this.saveButton.Visibility = this.mode != ConnectionDataMode.QuickConnect ? Visibility.Visible : Visibility.Collapsed;

            bool validated = true;

            if (this.mode != ConnectionDataMode.QuickConnect && this.nameTextBox.Text.Length == 0)
            {
                validated = false;
            }

            if (this.hostTextBox.Text.Length == 0)
            {
                validated = false;
            }

            int port;
            if (!int.TryParse(this.portTextBox.Text, out port))
            {
                validated = false;
            }

            if ((AuthenticationType)this.authenticationMethodComboBox.SelectedIndex == AuthenticationType.PrivateKey && this.privateKeyComboBox.SelectedIndex < 0)
            {
                validated = false;
            }

            this.connectButton.IsEnabled = validated;
            this.saveButton.IsEnabled = validated;
        }

        private void SetupPageTitle()
        {
            switch (this.mode)
            {
                case ConnectionDataMode.QuickConnect:
                    this.pageTitle.Text = "Quick Connect";
                    break;
                case ConnectionDataMode.New:
                    this.pageTitle.Text = "New Connection";
                    break;
                case ConnectionDataMode.Edit:
                    this.pageTitle.Text = "Edit Connection";
                    break;
                default:
                    this.pageTitle.Text = App.Current.Resources["AppName"] as string;
                    break;
            }
        }

        private void requiredField_Changed(object sender, TextChangedEventArgs e)
        {
            this.SetupAppBar();
        }

        private void sshRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            this.sshOptions.Visibility = Visibility.Visible;

            int port = 0;
            if (this.portTextBox.Text.Length == 0 || (int.TryParse(this.portTextBox.Text, out port) && port == DefaultTelnetPort))
            {
                this.portTextBox.Text = DefaultSshPort.ToString();
            }
        }

        private void sshRadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            this.sshOptions.Visibility = Visibility.Collapsed;
        }

        private void telnetRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            int port = 0;
            if (this.portTextBox.Text.Length == 0 || (int.TryParse(this.portTextBox.Text, out port) && port == DefaultSshPort))
            {
                this.portTextBox.Text = DefaultTelnetPort.ToString();
            }
        }

        private void authenticationMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var authenticationMethod = (AuthenticationType)this.authenticationMethodComboBox.SelectedIndex;
            if (authenticationMethod == AuthenticationType.PrivateKey)
            {
                this.privateKeyOptions.Visibility = Visibility.Visible;
            }
            else
            {
                this.privateKeyOptions.Visibility = Visibility.Collapsed;
            }

            if (authenticationMethod == AuthenticationType.PrivateKeyAgent)
            {
                this.privateKeyAgentOptions.Visibility = Visibility.Visible;
            }
            else
            {
                this.privateKeyAgentOptions.Visibility = Visibility.Collapsed;
            }

            this.SetupAppBar();
        }

        private void privateKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SetupAppBar();
        }

        private void cancelAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            this.navigationHelper.GoBack();
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
