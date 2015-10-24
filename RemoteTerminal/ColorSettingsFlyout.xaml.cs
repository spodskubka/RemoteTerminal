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

using RemoteTerminal.Model;
using RemoteTerminal.Screens;
using RemoteTerminal.Terminals;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace RemoteTerminal
{
    /// <summary>
    /// A settings flyout for color settings.
    /// </summary>
    public sealed partial class ColorSettingsFlyout : SettingsFlyout
    {
        /// <summary>
        /// The theme that is displayed.
        /// </summary>
        private ColorThemeData customTheme;

        /// <summary>
        /// A value indicating whether selection changes in the <see cref="ScreenColorListBox"/> should be ignored.
        /// </summary>
        /// <remarks>
        /// The purpose of this field is to prevent race conditions.
        /// </remarks>
        private bool ignoreScreenColorListBoxSelectionChanging = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorSettingsFlyout"/> class.
        /// </summary>
        public ColorSettingsFlyout()
        {
            this.InitializeComponent();

            this.customTheme = ColorThemesDataSource.GetCustomTheme();
            for (int i = 0; i < this.ScreenColorListBox.Items.Count; i++)
            {
                ListBoxItem item = (ListBoxItem)this.ScreenColorListBox.Items[i];

                int screenColor = i - 4;
                Color color = this.customTheme.ColorTable[(ScreenColor)screenColor];
                item.BorderBrush = new SolidColorBrush(color);
                item.BorderThickness = new Thickness(50.0d, 0.0d, 0.0d, 0.0d);
            }
            this.ScreenColorListBox.SelectedIndex = 0;

            this.FontFamilyListBox.Items.Clear();
            this.FontFamilyListBox.ItemsSource = ScreenDisplay.BaseLogicalFontMetrics.Keys;
            this.FontFamilyListBox.SelectedItem = this.customTheme.FontFamily;
            this.FontSizeSlider.Value = this.customTheme.FontSize;
        }

        /// <summary>
        /// Occurs when the reset font button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
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

        /// <summary>
        /// Occurs when the reset colors button is clicked.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
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

        /// <summary>
        /// Occurs when the currently selected item in the <see cref="FontFamilyListBox"/> changes.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void FontFamilyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.FontPreviewTextBlock.FontFamily = new FontFamily((string)this.FontFamilyListBox.SelectedItem);

            if (this.customTheme.FontFamily == (string)this.FontFamilyListBox.SelectedItem)
            {
                return;
            }

            this.customTheme.FontFamily = (string)this.FontFamilyListBox.SelectedItem;

            TerminalPageForceRender(fontChanged: true);
        }

        /// <summary>
        /// Occurs when the currently selected item in the <see cref="ScreenColorListBox"/> changes.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
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

        /// <summary>
        /// Occurs when the range value of the <see cref="FontSizeSlider"/> changes.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
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

        /// <summary>
        /// Occurs when the range value of one of the color sliders changes.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
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

            ListBoxItem item = (ListBoxItem)this.ScreenColorListBox.SelectedItem;
            ((SolidColorBrush)item.BorderBrush).Color = color;
            ((SolidColorBrush)this.ColorPreviewRectangle.Fill).Color = color;

            int screenColor = this.ScreenColorListBox.SelectedIndex - 4;

            if (this.customTheme.ColorTable[(ScreenColor)screenColor] == color)
            {
                return;
            }
            this.customTheme.ColorTable[(ScreenColor)screenColor] = color;

            TerminalPageForceRender(fontChanged: false);
        }

        /// <summary>
        /// Forces a redraw of the terminal screen, when the TerminalPage is currently active.
        /// </summary>
        /// <param name="fontChanged">A value indicating whether the font has changed (to recalculate the screen size).</param>
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

        /// <summary>
        /// Occurs when the <see cref="ColorSettingsFlyout"/> loses focus.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void SettingsFlyout_LostFocus(object sender, RoutedEventArgs e)
        {
            // Save the color theme settings
            var colorThemesDataSource = App.Current.Resources["colorThemesDataSource"] as ColorThemesDataSource;
            colorThemesDataSource.AddOrUpdate(colorThemesDataSource.CustomTheme);
        }
    }
}
