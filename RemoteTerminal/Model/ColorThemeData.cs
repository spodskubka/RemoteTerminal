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

using System.Collections.Generic;
using System.Linq;
using RemoteTerminal.Screens;
using RemoteTerminal.Terminals;
using Windows.UI;

namespace RemoteTerminal.Model
{
    /// <summary>
    /// Data model class for storing theme data.
    /// </summary>
    /// <remarks>
    /// Contrary to the name of this class it not only stores colors but also font family/size.
    /// </remarks>
    public class ColorThemeData
    {
        /// <summary>
        /// Gets or sets the font family associated with this theme.
        /// </summary>
        public string FontFamily { get; set; }

        /// <summary>
        /// Gets or sets the font size associated with this theme.
        /// </summary>
        public int FontSize { get; set; }

        /// <summary>
        /// Gets the color table associated with this theme.
        /// </summary>
        public Dictionary<ScreenColor, Color> ColorTable { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorThemeData"/> class.
        /// </summary>
        private ColorThemeData()
        {
            this.FontFamily = ScreenDisplay.BaseLogicalFontMetrics.Keys.First();
            this.FontSize = 0;

            this.ColorTable = new Dictionary<ScreenColor, Color>(260);

            // colors -4 to -1 are default fore-/background and cursor fore-/background
            this.ColorTable[ScreenColor.CursorBackground] = Colors.Green;
            this.ColorTable[ScreenColor.CursorForeground] = Colors.Black;
            this.ColorTable[ScreenColor.DefaultBackground] = Colors.Black;
            this.ColorTable[ScreenColor.DefaultForeground] = Colors.White;

            // colors 0 to 15 are the 8 default colors normal and bright
            this.ColorTable[ScreenColor.Black] = Colors.Black;
            this.ColorTable[ScreenColor.Red] = Colors.Red;
            this.ColorTable[ScreenColor.Green] = Colors.Green;
            this.ColorTable[ScreenColor.Yellow] = Colors.Yellow;
            this.ColorTable[ScreenColor.Blue] = Colors.Blue;
            this.ColorTable[ScreenColor.Magenta] = Colors.Magenta;
            this.ColorTable[ScreenColor.Cyan] = Colors.Cyan;
            this.ColorTable[ScreenColor.White] = Colors.White;
            this.ColorTable[ScreenColor.BlackBright] = Color.FromArgb(255, 85, 85, 85);
            this.ColorTable[ScreenColor.RedBright] = Color.FromArgb(255, 255, 85, 85);
            this.ColorTable[ScreenColor.GreenBright] = Color.FromArgb(255, 85, 255, 85);
            this.ColorTable[ScreenColor.YellowBright] = Color.FromArgb(255, 255, 255, 127);
            this.ColorTable[ScreenColor.BlueBright] = Color.FromArgb(255, 85, 85, 255);
            this.ColorTable[ScreenColor.MagentaBright] = Color.FromArgb(255, 255, 85, 255);
            this.ColorTable[ScreenColor.CyanBright] = Color.FromArgb(255, 127, 255, 255);
            this.ColorTable[ScreenColor.WhiteBright] = Color.FromArgb(255, 255, 255, 255);

            // colors 16 to 231 are a 6x6x6 color cube
            for (int r = 0; r < 6; r++)
            {
                for (int g = 0; g < 6; g++)
                {
                    for (int b = 0; b < 6; b++)
                    {
                        int colorIndex = 16 + (r * 36) + (g * 6) + b;
                        int red = r > 0 ? (r * 40) + 55 : 0;
                        int green = g > 0 ? (g * 40) + 55 : 0;
                        int blue = b > 0 ? (b * 40) + 55 : 0;
                        this.ColorTable[(ScreenColor)colorIndex] = Color.FromArgb(255, (byte)red, (byte)green, (byte)blue);
                    }
                }
            }

            // colors 232 to 255 are a grayscale ramp, intentionally leaving out black and white
            for (int gray = 0; gray < 24; gray++)
            {
                int colorIndex = 232 + gray;
                int level = (gray * 10) + 8;
                this.ColorTable[(ScreenColor)colorIndex] = Color.FromArgb(255, (byte)level, (byte)level, (byte)level);
            }
        }

        /// <summary>
        /// Creates the default theme.
        /// </summary>
        /// <returns>A new <see cref="ColorThemeData"/> instance representing the default theme.</returns>
        public static ColorThemeData CreateDefault()
        {
            return new ColorThemeData();
        }
    }
}
