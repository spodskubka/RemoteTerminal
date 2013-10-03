using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RemoteTerminal.Model;
using RemoteTerminal.Screens;
using RemoteTerminal.Terminals;
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

            this.FontFamilyListBox.Items.Clear();
            this.FontFamilyListBox.ItemsSource = ScreenDisplay.BaseLogicalFontMetrics.Keys;
            this.FontFamilyListBox.SelectedItem = this.customTheme.FontFamily;
            this.FontSizeSlider.Value = this.customTheme.FontSize;
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

        private void ResetFontClicked(object sender, RoutedEventArgs e)
        {
            var defaultTheme = ColorThemeData.CreateDefault();
            this.customTheme.FontFamily = defaultTheme.FontFamily;
            this.customTheme.FontSize = defaultTheme.FontSize;

            var colorThemesDataSource = App.Current.Resources["colorThemesDataSource"] as ColorThemesDataSource;
            colorThemesDataSource.AddOrUpdate(this.customTheme);

            this.FontFamilyListBox.SelectedItem = this.customTheme.FontFamily;
            this.FontSizeSlider.Value = this.customTheme.FontSize;

            TerminalPageForceRender(fontChanged: true);
        }

        private void ResetColorsClicked(object sender, RoutedEventArgs e)
        {
            var defaultTheme = ColorThemeData.CreateDefault();
            for (int i = -4; i < 16; i++)
            {
                this.customTheme.ColorTable[(ScreenColor)i] = defaultTheme.ColorTable[(ScreenColor)i];
            }

            var colorThemesDataSource = App.Current.Resources["colorThemesDataSource"] as ColorThemesDataSource;
            colorThemesDataSource.AddOrUpdate(this.customTheme);

            this.ScreenColorListBox_SelectionChanged(sender, null);

            TerminalPageForceRender(fontChanged: false);
        }

        private void FontFamilyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.FontPreviewTextBlock.FontFamily = new FontFamily((string)this.FontFamilyListBox.SelectedItem);

            if (this.customTheme.FontFamily == this.FontFamilyListBox.SelectedItem)
            {
                return;
            }

            this.customTheme.FontFamily = (string)this.FontFamilyListBox.SelectedItem;

            TerminalPageForceRender(fontChanged: true);
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

        private void FontSizeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            this.FontPreviewTextBlock.FontSize = ScreenDisplay.BaseLogicalFontMetrics[this.customTheme.FontFamily].FontSize * (1 + (ScreenDisplay.FontSizeScalingFactor * (float)this.FontSizeSlider.Value));

            if (this.customTheme.FontSize == this.FontSizeSlider.Value)
            {
                return;
            }

            this.customTheme.FontSize = (int)this.FontSizeSlider.Value;

            TerminalPageForceRender(fontChanged: true);
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

            ((SolidColorBrush)this.ColorPreviewRectangle.Fill).Color = color;

            int screenColor = this.ScreenColorListBox.SelectedIndex - 4;

            if (this.customTheme.ColorTable[(ScreenColor)screenColor] == color)
            {
                return;
            }
            this.customTheme.ColorTable[(ScreenColor)screenColor] = color;

            TerminalPageForceRender(fontChanged: false);
        }

        private static void TerminalPageForceRender(bool fontChanged)
        {
            var frame = Window.Current.Content as Frame;
            if (frame != null)
            {
                var terminalPage = frame.Content as TerminalPage;
                if (terminalPage != null)
                {
                    terminalPage.ForceRender(fontChanged);
                }
            }
        }
    }
}
