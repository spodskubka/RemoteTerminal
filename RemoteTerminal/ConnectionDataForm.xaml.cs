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

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace RemoteTerminal
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class ConnectionDataForm : RemoteTerminal.Common.LayoutAwarePage
    {
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
            this.mode = ConnectionDataMode.Edit;

            string id = navigationParameter as string;

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

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
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

            this.Frame.Navigate(typeof(TerminalPage), connectionData);
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

            this.SetupAppBar();
        }

        private void privateKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SetupAppBar();
        }
    }
}
