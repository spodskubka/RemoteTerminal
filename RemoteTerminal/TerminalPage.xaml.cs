using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RemoteTerminal.Connections;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class TerminalPage : RemoteTerminal.Common.LayoutAwarePage
    {
        Guid? terminalGuid = null;

        public TerminalPage()
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
            if (navigationParameter == null)
            {
                this.Frame.GoBack();
            }

            ConnectionData connectionData;
            if (navigationParameter is string)
            {
                connectionData = FavoritesDataSource.GetFavorite((string)navigationParameter);
                if (connectionData == null)
                {
                    this.Frame.GoBack();
                    return;
                }
            }
            else if (navigationParameter is ConnectionData)
            {
                connectionData = navigationParameter as ConnectionData;
            }
            else
            {
                this.Frame.GoBack();
                return;
            }

            this.DefaultViewModel["connection"] = connectionData;

            this.terminalGuid = TerminalManager.Create(connectionData);
            this.screenDisplay.AssignTerminal(this.terminalGuid.Value);
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

        protected override void GoBack(object sender, RoutedEventArgs e)
        {
            if (this.screenDisplay != null)
            {
                this.screenDisplay.Dispose();
                this.screenDisplay = null;
            }

            if (this.terminalGuid != null)
            {
                TerminalManager.Remove(this.terminalGuid.Value);
                this.terminalGuid = null;
            }

            base.GoBack(sender, e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (this.screenDisplay != null)
            {
                this.screenDisplay.Dispose();
                this.screenDisplay = null;
            }

            if (this.terminalGuid != null)
            {
                TerminalManager.Remove(this.terminalGuid.Value);
                this.terminalGuid = null;
            }

            base.OnNavigatedFrom(e);
        }
    }
}
