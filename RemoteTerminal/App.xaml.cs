// Remote Terminal, an SSH/Telnet terminal emulator for Microsoft Windows
// Copyright (C) 2012-2015 Stefan Podskubka
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using RemoteTerminal.Common;
using RemoteTerminal.Model;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        /// Initializes the singleton Application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application creates a window.
        /// </summary>
        /// <param name="args">Event data for the event.</param>
        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            base.OnWindowCreated(args);
            SettingsPane.GetForCurrentView().CommandsRequested += App_CommandsRequested;
        }

        /// <summary>
        /// The event handler that is called when the user selects the "Font and Colors" command in the settings pane.
        /// </summary>
        /// <param name="command">Represents the invoked command.</param>
        void OnColorsCommand(IUICommand command)
        {
            ColorSettingsFlyout mypane = new ColorSettingsFlyout();
            //mypane.Width = settingsWidth;
            mypane.Show();
        }

        /// <summary>
        /// Occurs when the user opens the settings pane. Initializes the setting commands.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event data.</param>
        void App_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            SettingsCommand colorsCommand = new SettingsCommand("colors", "Font and Colors", OnColorsCommand);

            SettingsCommand privacyPolicyCommand = new SettingsCommand("privacyPolicy", "Privacy Policy", async (x) =>
            {
                await Launcher.LaunchUriAsync(new Uri("http://stefanpodskubkadev.blogspot.co.at/p/remote-terminal-privacy-policy.html"));
            });

            SettingsCommand helpCommand = new SettingsCommand("help", "Help", async (x) =>
            {
                await Launcher.LaunchUriAsync(new Uri("http://stefanpodskubkadev.blogspot.co.at/p/contact.html"));
            });

            args.Request.ApplicationCommands.Add(colorsCommand);
            args.Request.ApplicationCommands.Add(privacyPolicyCommand);
            args.Request.ApplicationCommands.Add(helpCommand);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user. Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active

            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                //Associate the frame with a SuspensionManager key
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

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
                        await privateKeysDataSource.GetPrivateKeys();
                    }
                }

                ColorThemesDataSource colorThemesDataSource = (ColorThemesDataSource)App.Current.Resources["colorThemesDataSource"];
                if (colorThemesDataSource != null)
                {
                    if (colorThemesDataSource.CustomTheme == null)
                    {
                        colorThemesDataSource.GetColorThemes();
                    }
                }

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        //Something went wrong restoring state.
                        //Assume there is no state and continue
                    }
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(FavoritesPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            // The TerminalPage doesn't really support suspending, the other pages don't require it...
            //await SuspensionManager.SaveAsync();
            deferral.Complete();
        }

        /// <summary>
        /// Handles the request to set the version of the application data in the application data store.
        /// </summary>
        /// <param name="request">The set version request.</param>
        /// <remarks>
        /// Application data migration should be done here.
        /// </remarks>
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
