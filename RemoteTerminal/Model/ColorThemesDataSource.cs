using System;
using System.Linq;
using RemoteTerminal.Screens;
using Windows.Data.Json;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;

namespace RemoteTerminal.Model
{
    /// <summary>
    /// The data source for theme data.
    /// </summary>
    /// <remarks>
    /// Real theme support is still missing from the app. The global font/color settings that are used for all
    /// terminals are stored with the theme name "CustomTheme". Other themes don't exist.
    /// Themes are stored as roaming app settings.
    /// </remarks>
    internal class ColorThemesDataSource
    {
        /// <summary>
        /// The global font/color settings.
        /// </summary>
        public ColorThemeData CustomTheme { get; private set; }

        /// <summary>
        /// Reads all themes from the roaming app settings.
        /// </summary>
        public void GetColorThemes()
        {
            var colorThemes = GetColorThemesSettings();
            if (!colorThemes.ContainsKey("CustomTheme"))
            {
                this.CustomTheme = ColorThemeData.CreateDefault();
                return;
            }

            ColorThemeData colorThemeData = ColorThemeData.CreateDefault();

            try
            {
                string colorThemeJsonString = (string)colorThemes["CustomTheme"];
                JsonObject jsonObject = JsonObject.Parse(colorThemeJsonString);

                colorThemeData.FontFamily = jsonObject.ContainsKey("FontFamily") ? jsonObject.GetNamedString("FontFamily") : colorThemeData.FontFamily;
                colorThemeData.FontSize = jsonObject.ContainsKey("FontSize") ? (int)jsonObject.GetNamedNumber("FontSize") : colorThemeData.FontSize;

                JsonObject jsonColorTable = jsonObject.GetNamedObject("ColorTable");
                foreach (var jsonColorTableEntry in jsonColorTable)
                {
                    ScreenColor screenColor;
                    if (!Enum.TryParse<ScreenColor>(jsonColorTableEntry.Key, out screenColor))
                    {
                        continue;
                    }

                    if (jsonColorTableEntry.Value.ValueType != JsonValueType.Number)
                    {
                        continue;
                    }

                    colorThemeData.ColorTable[screenColor] = DoubleToColor(jsonColorTableEntry.Value.GetNumber());
                }
            }
            catch (Exception)
            {
                // A color theme seems to contain invalid data, ignore it, don't delete it.
                // Maybe a future update is able to read the data.
                //continue;
            }

            this.CustomTheme = colorThemeData;
        }

        /// <summary>
        /// Converts a double value to a <see cref="Color"/> object.
        /// </summary>
        /// <param name="doubleColor">The double color value.</param>
        /// <returns>The resulting <see cref="Color"/> object.</returns>
        private static Color DoubleToColor(double doubleColor)
        {
            int intColor = (int)doubleColor;
            return Color.FromArgb((byte)(intColor >> 24), (byte)(intColor >> 16), (byte)(intColor >> 8), (byte)intColor);
        }

        /// <summary>
        /// Converts a <see cref="Color"/> object to a double value.
        /// </summary>
        /// <param name="color">The <see cref="Color"/> object.</param>
        /// <returns>The resulting double value.</returns>
        private static double ColorToDouble(Color color)
        {
            int intColor = color.A << 24 | color.R << 16 | color.G << 8 | color.B;
            return (double)intColor;
        }

        /// <summary>
        /// Reads the roaming "ColorThemes" app settings container.
        /// </summary>
        /// <returns>The values from the roaming "ColorThemes" app settings container.</returns>
        private static IPropertySet GetColorThemesSettings()
        {
            var colorThemesContainer = ApplicationData.Current.RoamingSettings.CreateContainer("ColorThemes", ApplicationDataCreateDisposition.Always);
            return colorThemesContainer.Values;
        }

        /// <summary>
        /// Adds or updates a theme.
        /// </summary>
        /// <param name="colorThemeData">The theme to update.</param>
        /// <remarks>
        /// Just updates the one and only "CustomTheme". No other themes are supported at the moment.
        /// </remarks>
        public void AddOrUpdate(ColorThemeData colorThemeData)
        {
            var colorThemes = GetColorThemesSettings();

            //if (connectionData.Id == null)
            //{
            //    connectionData.Id = Guid.NewGuid().ToString();
            //}

            JsonObject jsonObject = new JsonObject();

            jsonObject.Add("FontFamily", JsonValue.CreateStringValue(colorThemeData.FontFamily));
            jsonObject.Add("FontSize", JsonValue.CreateNumberValue(colorThemeData.FontSize));

            JsonObject jsonColorTable = new JsonObject();
            foreach (var colorTableEntry in colorThemeData.ColorTable.Where(c => (int)c.Key < 16))
            {
                jsonColorTable.Add(colorTableEntry.Key.ToString(), JsonValue.CreateNumberValue(ColorToDouble(colorTableEntry.Value)));
            }

            jsonObject.Add("ColorTable", jsonColorTable);

            string colorThemeJsonString = jsonObject.Stringify();

            colorThemes["CustomTheme"] = colorThemeJsonString;

            this.CustomTheme = colorThemeData;
        }

        /// <summary>
        /// Returns the global (custom) theme.
        /// </summary>
        /// <returns>The global (custom) theme.</returns>
        public static ColorThemeData GetCustomTheme()
        {
            var colorThemesDataSource = App.Current.Resources["colorThemesDataSource"] as ColorThemesDataSource;

            return colorThemesDataSource.CustomTheme;
        }
    }
}
