using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RemoteTerminal.Model;
using RemoteTerminal.Screens;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RemoteTerminal
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ColorSettingsFlyout : Page
    {
        private ColorThemeData customTheme;
        private bool ignoreScreenColorListBoxSelectionChanging = false;

        public ColorSettingsFlyout()
        {
            this.InitializeComponent();

            this.customTheme = ColorThemesDataSource.GetCustomTheme();
            this.ScreenColorListBox.SelectedIndex = 0;
        }

        /// <summary>
        /// This is the click handler for the back button on the Flyout.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsBackClicked(object sender, RoutedEventArgs e)
        {
            // First close our Flyout.
            Popup parent = this.Parent as Popup;
            if (parent != null)
            {
                parent.IsOpen = false;
            }

            // If the app is not snapped, then the back button shows the Settings pane again.
            if (Windows.UI.ViewManagement.ApplicationView.Value != Windows.UI.ViewManagement.ApplicationViewState.Snapped)
            {
                SettingsPane.Show();
            }
        }

        private void ResetClicked(object sender, RoutedEventArgs e)
        {
            var defaultTheme = ColorThemeData.CreateDefault();
            for (int i = -4; i < 16; i++)
            {
                this.customTheme.ColorTable[(ScreenColor)i] = defaultTheme.ColorTable[(ScreenColor)i];
            }

            var colorThemesDataSource = App.Current.Resources["colorThemesDataSource"] as ColorThemesDataSource;
            colorThemesDataSource.AddOrUpdate(this.customTheme);

            this.ScreenColorListBox_SelectionChanged(sender, null);

            TerminalPageForceRender();
        }

        private void ScreenColorListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int screenColor = this.ScreenColorListBox.SelectedIndex - 4;
            Color color = this.customTheme.ColorTable[(ScreenColor)screenColor];

            this.ignoreScreenColorListBoxSelectionChanging = true;
            this.RedSlider.Value = color.R;
            this.GreenSlider.Value = color.G;
            this.BlueSlider.Value = color.B;
            this.ignoreScreenColorListBoxSelectionChanging = false;
            this.ColorSlider_ValueChanged(sender, null);
        }

        private void ColorSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (this.ignoreScreenColorListBoxSelectionChanging)
            {
                return;
            }

            var color = new Color()
            {
                R = (byte)this.RedSlider.Value,
                G = (byte)this.GreenSlider.Value,
                B = (byte)this.BlueSlider.Value,
                A = 255,
            };

            ((SolidColorBrush)this.PreviewRectangle.Fill).Color = color;

            int screenColor = this.ScreenColorListBox.SelectedIndex - 4;

            if (this.customTheme.ColorTable[(ScreenColor)screenColor] == color)
            {
                return;
            }
            this.customTheme.ColorTable[(ScreenColor)screenColor] = color;

            TerminalPageForceRender();
        }

        private static void TerminalPageForceRender()
        {
            var frame = Window.Current.Content as Frame;
            if (frame != null)
            {
                var terminalPage = frame.Content as TerminalPage;
                if (terminalPage != null)
                {
                    terminalPage.ForceRender();
                }
            }
        }
    }
}
