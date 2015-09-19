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
    public sealed partial class ColorSettingsFlyout : SettingsFlyout
    {
        private ColorThemeData customTheme;
        private bool ignoreScreenColorListBoxSelectionChanging = false;

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

            if (this.customTheme.FontFamily == (string)this.FontFamilyListBox.SelectedItem)
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

        private void SettingsFlyout_LostFocus(object sender, RoutedEventArgs e)
        {
            // Save the color theme settings
            var colorThemesDataSource = App.Current.Resources["colorThemesDataSource"] as ColorThemesDataSource;
            colorThemesDataSource.AddOrUpdate(colorThemesDataSource.CustomTheme);
        }
    }
}
