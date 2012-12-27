using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RemoteTerminal.Model;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace RemoteTerminal
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            base.OnWindowCreated(args);
            SettingsPane.GetForCurrentView().CommandsRequested += App_CommandsRequested;
        }

        void App_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            SettingsCommand privacyPolicyCommand = new SettingsCommand("privacyPolicy", "Privacy Policy", async (x) =>
            {
                await Launcher.LaunchUriAsync(new Uri("http://stefanpodskubkadev.blogspot.co.at/p/remote-terminal-privacy-policy.html"));
            });

            SettingsCommand helpCommand = new SettingsCommand("help", "Help", async (x) =>
            {
                await Launcher.LaunchUriAsync(new Uri("http://stefanpodskubkadev.blogspot.co.at/p/contact.html"));
            });

            args.Request.ApplicationCommands.Add(privacyPolicyCommand);
            args.Request.ApplicationCommands.Add(helpCommand);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                await ApplicationData.Current.SetVersionAsync(0, SetVersionHandler);

                FavoritesDataSource favoritesDataSource = (FavoritesDataSource)App.Current.Resources["favoritesDataSource"];
                if (favoritesDataSource != null)
                {
                    if (favoritesDataSource.Favorites.Count == 0)
                    {
                        favoritesDataSource.GetFavorites();
                    }
                }

                PrivateKeysDataSource privateKeysDataSource = (PrivateKeysDataSource)App.Current.Resources["privateKeysDataSource"];
                if (privateKeysDataSource != null)
                {
                    if (privateKeysDataSource.PrivateKeys.Count == 0)
                    {
                        privateKeysDataSource.GetPrivateKeys();
                    }
                }

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(FavoritesPage), args.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        void SetVersionHandler(SetVersionRequest request)
        {
            SetVersionDeferral deferral = request.GetDeferral();

            // Downgrades are not supported...
            if (request.DesiredVersion < request.CurrentVersion)
            {
                ApplicationData.Current.ClearAsync().AsTask().Wait();
            }

            //if (request.CurrentVersion < 1 && request.DesiredVersion >= 1)
            //{
            //}

            //if (request.CurrentVersion < 2)
            //{
            //}

            //if (request.CurrentVersion < 3)
            //{
            //}

            deferral.Complete();
        }
    }
}
