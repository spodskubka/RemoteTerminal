using System;
using System.Linq;
using RemoteTerminal.Common;
using RemoteTerminal.Model;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace RemoteTerminal
{
    /// <summary>
    /// The page where connection data can be edited.
    /// </summary>
    public sealed partial class ConnectionDataForm : Page
    {
        /// <summary>
        /// The <see cref="NavigationHelper"/> for this page.
        /// </summary>
        private NavigationHelper navigationHelper;

        /// <summary>
        /// The default view model.
        /// </summary>
        /// <remarks>
        /// TODO: Is this used or can it be removed?
        /// </remarks>
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
        /// The default SSH port.
        /// </summary>
        private const int DefaultSshPort = 22;

        /// <summary>
        /// The default Telnet port.
        /// </summary>
        private const int DefaultTelnetPort = 23;

        /// <summary>
        /// The different modes for opening this page.
        /// </summary>
        enum ConnectionDataMode
        {
            /// <summary>
            /// The page was called to establish a quick connection.
            /// </summary>
            QuickConnect,

            /// <summary>
            /// The page was called to create a new favorite.
            /// </summary>
            New,

            /// <summary>
            /// The page was called to edit an existing favorite.
            /// </summary>
            Edit,
        }

        /// <summary>
        /// The id of the connection data.
        /// </summary>
        private string id;

        /// <summary>
        /// The mode for opening this page.
        /// </summary>
        private ConnectionDataMode mode;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionDataForm"/> class.
        /// </summary>
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

        /// <summary>
        /// Occurs when the save button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
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

        /// <summary>
        /// Occurs when the connect button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            var connectionData = this.CreateConnectionDataFromForm();

            Guid guid = TerminalManager.Create(connectionData);
            this.Frame.Navigate(typeof(TerminalPage), guid);
        }

        /// <summary>
        /// Creates a new <see cref="ConnectionData"/> object from the input fields.
        /// </summary>
        /// <returns>The created <see cref="ConnectionData"/> object.</returns>
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

        /// <summary>
        /// Sets up the <see cref="AppBar"/>(s).
        /// </summary>
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

        /// <summary>
        /// Sets up the page title.
        /// </summary>
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

        /// <summary>
        /// Occurs when one of the required fields changes.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void requiredField_Changed(object sender, TextChangedEventArgs e)
        {
            this.SetupAppBar();
        }

        /// <summary>
        /// Occurs when the SSH radio button is checked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void sshRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            this.sshOptions.Visibility = Visibility.Visible;

            int port = 0;
            if (this.portTextBox.Text.Length == 0 || (int.TryParse(this.portTextBox.Text, out port) && port == DefaultTelnetPort))
            {
                this.portTextBox.Text = DefaultSshPort.ToString();
            }
        }

        /// <summary>
        /// Occurs when the SSH radio button is unchecked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void sshRadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            this.sshOptions.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Occurs when the Telnet radio button is checked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void telnetRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            int port = 0;
            if (this.portTextBox.Text.Length == 0 || (int.TryParse(this.portTextBox.Text, out port) && port == DefaultSshPort))
            {
                this.portTextBox.Text = DefaultTelnetPort.ToString();
            }
        }

        /// <summary>
        /// Occurs when the currently selected item in the authentication method ComboBox changes.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
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

        /// <summary>
        /// Occurs when the currently selected item in the private key ComboBox changes.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void privateKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SetupAppBar();
        }

        /// <summary>
        /// Occurs when the cancel button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
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
