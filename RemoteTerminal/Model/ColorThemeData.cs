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
        public Color CursorForegroundColor { get; set; }
        public Color CursorBackgroundColor { get; set; }

        public Dictionary<ScreenColor, Color> ColorTable { get; private set; }

        public ColorThemeData()
        {
            this.ColorTable = new Dictionary<ScreenColor, Color>(259);

            // colors 16-231 are a 6x6x6 color cube
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

            // colors 232-255 are a grayscale ramp, intentionally leaving out black and white
            for (int gray = 0; gray < 24; gray++)
            {
                int colorIndex = 232 + gray;
                int level = (gray * 10) + 8;
                this.ColorTable[(ScreenColor)colorIndex] = Color.FromArgb(255, (byte)level, (byte)level, (byte)level);
            }
        }

        public static ColorThemeData CreateDefault()
        {
            ColorThemeData theme = new ColorThemeData();

            theme.CursorForegroundColor = Colors.Black;
            theme.CursorBackgroundColor = Colors.Green;

            theme.ColorTable[ScreenColor.DefaultBackground] = Colors.Black;
            theme.ColorTable[ScreenColor.DefaultForeground] = Colors.White;
            theme.ColorTable[ScreenColor.Black] = Colors.Black;
            theme.ColorTable[ScreenColor.Red] = Colors.Red;
            theme.ColorTable[ScreenColor.Green] = Colors.Green;
            theme.ColorTable[ScreenColor.Yellow] = Colors.Yellow;
            theme.ColorTable[ScreenColor.Blue] = Colors.Blue;
            theme.ColorTable[ScreenColor.Magenta] = Colors.Magenta;
            theme.ColorTable[ScreenColor.Cyan] = Colors.Cyan;
            theme.ColorTable[ScreenColor.White] = Colors.White;
            theme.ColorTable[ScreenColor.BlackBright] = Color.FromArgb(255, 85, 85, 85);
            theme.ColorTable[ScreenColor.RedBright] = Color.FromArgb(255, 255, 85, 85);
            theme.ColorTable[ScreenColor.GreenBright] = Color.FromArgb(255, 85, 255, 85);
            theme.ColorTable[ScreenColor.YellowBright] = Color.FromArgb(255, 255, 255, 127);
            theme.ColorTable[ScreenColor.BlueBright] = Color.FromArgb(255, 85, 85, 255);
            theme.ColorTable[ScreenColor.MagentaBright] = Color.FromArgb(255, 255, 85, 255);
            theme.ColorTable[ScreenColor.CyanBright] = Color.FromArgb(255, 127, 255, 255);
            theme.ColorTable[ScreenColor.WhiteBright] = Color.FromArgb(255, 255, 255, 255);

            return theme;
        }
    }
}
