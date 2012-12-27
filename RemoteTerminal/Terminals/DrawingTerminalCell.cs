using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace RemoteTerminal.Terminals
{
    public sealed partial class DrawingTerminalCell
    {
        private static readonly Color DefaultForegroundColor = Colors.White;
        private static readonly Color DefaultBackgroundColor = Colors.Black;

        private readonly DrawingTerminalDisplay display;

        public DrawingTerminalCell(DrawingTerminalDisplay display)
        {
            this.display = display;

            this.Reset();
        }

        public void Reset()
        {
            this.Character = ' ';
            this.Modifications = DrawingTerminalCellModifications.None;
            this.ForegroundColor = DefaultForegroundColor;
            this.BackgroundColor = DefaultBackgroundColor;
        }

        public override string ToString()
        {
            return this.Character.ToString();
        }

        public void ApplyFormat(DrawingTerminalCellFormat format)
        {
            this.Modifications = DrawingTerminalCellModifications.None;
            if (format.BoldMode)
            {
                this.Modifications |= DrawingTerminalCellModifications.Bold;
            }

            if (format.UnderlineMode)
            {
                this.Modifications |= DrawingTerminalCellModifications.Underline;
            }

            if (format.ReverseMode)
            {
                this.BackgroundColor = format.ForegroundColor;
                this.ForegroundColor = format.BackgroundColor;
            }
            else
            {
                this.BackgroundColor = format.BackgroundColor;
                this.ForegroundColor = format.ForegroundColor;
            }
        }

        public DrawingTerminalCell Clone()
        {
            return new DrawingTerminalCell(this.display)
            {
                Character = this.Character,
                Modifications = this.Modifications,
                ForegroundColor = this.ForegroundColor,
                BackgroundColor = this.BackgroundColor,
            };
        }

        public char Character { get; set; }
        public DrawingTerminalCellModifications Modifications { get; set; }
        public Color ForegroundColor { get; set; }
        public Color BackgroundColor { get; set; }
    }
}
