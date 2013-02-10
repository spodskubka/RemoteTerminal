using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace RemoteTerminal.Screens
{
    public class ScreenColorTheme
    {
        public static Lazy<ScreenColorTheme> defaultTheme = new Lazy<ScreenColorTheme>(() =>
        {
            ScreenColorTheme theme = new ScreenColorTheme();

            theme.ColorTable[ScreenColor.DefaultBackground] = Color.Black;
            theme.ColorTable[ScreenColor.DefaultForeground] = Color.White;
            theme.ColorTable[ScreenColor.Black] = Color.Black;
            theme.ColorTable[ScreenColor.Red] = Color.Red;
            theme.ColorTable[ScreenColor.Green] = Color.Green;
            theme.ColorTable[ScreenColor.Yellow] = Color.Yellow;
            theme.ColorTable[ScreenColor.Blue] = Color.Blue;
            theme.ColorTable[ScreenColor.Magenta] = Color.Magenta;
            theme.ColorTable[ScreenColor.Cyan] = Color.Cyan;
            theme.ColorTable[ScreenColor.White] = Color.White;
            theme.ColorTable[ScreenColor.BlackBright] = new Color(85, 85, 85, 255);
            theme.ColorTable[ScreenColor.RedBright] = new Color(255, 85, 85, 255);
            theme.ColorTable[ScreenColor.GreenBright] = new Color(85, 255, 85, 255);
            theme.ColorTable[ScreenColor.YellowBright] = new Color(255, 255, 127, 255);
            theme.ColorTable[ScreenColor.BlueBright] = new Color(85, 85, 255, 255);
            theme.ColorTable[ScreenColor.MagentaBright] = new Color(255, 85, 255, 255);
            theme.ColorTable[ScreenColor.CyanBright] = new Color(127, 255, 255, 255);
            theme.ColorTable[ScreenColor.WhiteBright] = new Color(255, 255, 255, 255);

            return theme;
        });

        public Color CursorForegroundColor { get { return Color.Black; } }
        public Color CursorBackgroundColor { get { return Color.Green; } }

        public Dictionary<ScreenColor, Color> ColorTable { get; private set; }

        public ScreenColorTheme()
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
                        this.ColorTable[(ScreenColor)colorIndex] = new Color((byte)red, (byte)green, (byte)blue, 255);
                    }
                }
            }

            // colors 232-255 are a grayscale ramp, intentionally leaving out black and white
            for (int gray = 0; gray < 24; gray++)
            {
                int colorIndex = 232 + gray;
                int level = (gray * 10) + 8;
                this.ColorTable[(ScreenColor)colorIndex] = new Color((byte)level, (byte)level, (byte)level, 255);
            }
        }

        public static ScreenColorTheme Default
        {
            get
            {
                return defaultTheme.Value;
            }
        }
    }
}
