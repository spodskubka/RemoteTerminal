using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using RemoteTerminal.Screens;
using Windows.Data.Json;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;

namespace RemoteTerminal.Model
{
    internal class ColorThemesDataSource
    {
        public ColorThemeData CustomTheme { get; private set; }

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

        private static Color DoubleToColor(double doubleColor)
        {
            int intColor = (int)doubleColor;
            return Color.FromArgb((byte)(intColor >> 24), (byte)(intColor >> 16), (byte)(intColor >> 8), (byte)intColor);
        }

        private static double ColorToDouble(Color color)
        {
            int intColor = color.A << 24 | color.R << 16 | color.G << 8 | color.B;
            return (double)intColor;
        }

        private static IPropertySet GetColorThemesSettings()
        {
            var colorThemesContainer = ApplicationData.Current.RoamingSettings.CreateContainer("ColorThemes", ApplicationDataCreateDisposition.Always);
            return colorThemesContainer.Values;
        }

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

        // Returns the custom theme.
        public static ColorThemeData GetCustomTheme()
        {
            // Simple linear search is acceptable for small data sets
            var colorThemesDataSource = App.Current.Resources["colorThemesDataSource"] as ColorThemesDataSource;

            return colorThemesDataSource.CustomTheme;
        }
    }
}
