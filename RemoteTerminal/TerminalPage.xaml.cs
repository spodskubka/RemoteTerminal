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
using Windows.UI.Core;
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
    public sealed partial class TerminalPage : RemoteTerminal.Common.LayoutAwarePage
    {
        private InputPaneHelper inputPaneHelper;

        public static readonly DependencyProperty TerminalProperty = DependencyProperty.Register("Terminal", typeof(ITerminal), typeof(TerminalPage), null);
        public ITerminal Terminal
        {
            get { return (ITerminal)this.GetValue(TerminalProperty); }
            set
            {
                this.SetValue(TerminalProperty, value);
                if (this.screenDisplay != null)
                {
                    this.screenDisplay.AssignTerminal(value);
                }
            }
        }

        public TerminalPage()
        {
            this.InitializeComponent();

            // InputPaneHelper is a custom class that allows keyboard event listeners to
            // be attached to individual elements
            this.inputPaneHelper = new InputPaneHelper();
            this.inputPaneHelper.SubscribeToKeyboard(true);
            this.inputPaneHelper.AddShowingHandler(this.screenDisplay, new InputPaneShowingHandler(CustomKeyboardHandler));
            this.inputPaneHelper.SetHidingHandler(new InputPaneHidingHandler(InputPaneHiding));
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
        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            if (navigationParameter == null)
            {
                this.Frame.GoBack();
            }

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                ConnectionData connectionData;
                if (navigationParameter is string)
                {
                    connectionData = FavoritesDataSource.GetFavorite((string)navigationParameter);
                    if (connectionData == null)
                    {
                        this.Frame.GoBack();
                        return;
                    }

                    this.Terminal = TerminalManager.Create(connectionData);
                }
                else if (navigationParameter is ConnectionData)
                {
                    connectionData = navigationParameter as ConnectionData;
                    this.Terminal = TerminalManager.Create(connectionData);
                }
                else if (navigationParameter is ITerminal)
                {
                    this.Terminal = navigationParameter as ITerminal;
                }
                else
                {
                    this.Frame.GoBack();
                    return;
                }
            });

            this.previewGrid.ItemsSource = TerminalManager.Terminals;
            this.TopAppBar.IsOpen = true;
            await Task.Delay(1000);
            this.TopAppBar.IsOpen = false;
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
            base.GoBack(sender, e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (this.inputPaneHelper != null)
            {
                inputPaneHelper.SubscribeToKeyboard(false);
                inputPaneHelper.RemoveShowingHandler(this.screenDisplay);
                inputPaneHelper.SetHidingHandler(null);
            }

            if (this.screenDisplay != null)
            {
                this.screenDisplay.Dispose();
                this.screenDisplay = null;
            }

            if (this.Terminal != null)
            {
                if (!this.Terminal.IsConnected)
                {
                    TerminalManager.Remove(this.Terminal);
                }
            }

            base.OnNavigatedFrom(e);
        }

        private void PreviewGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            ITerminal terminal = e.ClickedItem as ITerminal;
            if (terminal == null)
            {
                return;
            }

            this.Terminal = terminal;

            this.TopAppBar.IsOpen = false;
        }

        private void PreviewGrid_ItemCloseButtonClick(object sender, RoutedEventArgs e)
        {
            ITerminal terminal = ((Button)sender).Tag as ITerminal;
            if (this.Terminal == terminal)
            {
                var switchToTerminal = TerminalManager.Terminals.Where(t => t != terminal).FirstOrDefault();
                if (switchToTerminal == null)
                {
                    GoBack(sender, e);
                }
                else
                {
                    this.Terminal = switchToTerminal;
                }
            }

            TerminalManager.Remove(terminal);
        }

        private void CustomKeyboardHandler(object sender, InputPaneVisibilityEventArgs e)
        {
            this.ContentGrid.VerticalAlignment = VerticalAlignment.Top;
            this.ContentGrid.Height = e.OccludedRect.Y;

            // Be careful with this property. Once it has been set, the framework will
            // do nothing to help you keep the focused element in view.
            e.EnsuredFocusedElementInView = true;
        }

        private void InputPaneHiding(InputPane sender, InputPaneVisibilityEventArgs e)
        {
            this.ContentGrid.VerticalAlignment = VerticalAlignment.Stretch;
            this.ContentGrid.Height = Double.NaN;

            // Be careful with this property. Once it has been set, the framework will not change
            // any layouts in response to the keyboard coming up
            e.EnsuredFocusedElementInView = true;
        }
    }
}
