using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteTerminal.Screens;
using Windows.UI;

namespace RemoteTerminal.Model
{
    public class ColorThemeData
    {
        public Dictionary<ScreenColor, Color> ColorTable { get; private set; }

        private ColorThemeData()
        {
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

        public static ColorThemeData CreateDefault()
        {
            return new ColorThemeData();
        }
    }
}
