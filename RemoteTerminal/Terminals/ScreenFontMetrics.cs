namespace RemoteTerminal.Terminals
{
    public class ScreenFontMetrics
    {
        public ScreenFontMetrics(float fontSize, float cellWidth, float cellHeight)
        {
            this.FontSize = fontSize;
            this.CellWidth = cellWidth;
            this.CellHeight = cellHeight;
        }

        public float FontSize { get; private set; }
        public float CellWidth { get; private set; }
        public float CellHeight { get; private set; }

        public static ScreenFontMetrics operator *(ScreenFontMetrics fontMetrics, float d)
        {
            return new ScreenFontMetrics(fontMetrics.FontSize * d, fontMetrics.CellWidth * d, fontMetrics.CellHeight * d);
        }
    }
}
