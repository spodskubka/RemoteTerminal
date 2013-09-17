using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Linq;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System;

namespace RemoteTerminal.Screens
{
    public class ScreenCell : IRenderableScreenCell
    {
        private const int RecyclerSize = 10000;

        private static readonly object RecyclableCellsLock = new object();
        private static readonly List<ScreenCell> RecyclableCells = new List<ScreenCell>(RecyclerSize);

        public static IEnumerable<ScreenCell> GetFreshCells(int count)
        {
            List<ScreenCell> cellsRecycled;
            lock (RecyclableCellsLock)
            {
                cellsRecycled = RecyclableCells.GetRange(0, Math.Min(count, RecyclableCells.Count));
                RecyclableCells.RemoveRange(0, cellsRecycled.Count);
            }

            foreach (var cell in cellsRecycled)
            {
                cell.Reset();
            }

            IEnumerable<ScreenCell> cellsNonRecycled = new int[count - cellsRecycled.Count].Select(c => new ScreenCell());

            return cellsRecycled.Concat(cellsNonRecycled);
        }

        public static void RecycleCells(IEnumerable<ScreenCell> cells)
        {
            lock (RecyclableCellsLock)
            {
                if (RecyclableCells.Count >= RecyclerSize)
                {
                    return;
                }

                RecyclableCells.AddRange(cells);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ScreenCell()
        {
            this.Reset();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Reset()
        {
            this.Character = ' ';
            this.Modifications = ScreenCellModifications.None;
            this.ForegroundColor = ScreenColor.DefaultForeground;
            this.BackgroundColor = ScreenColor.DefaultBackground;
        }

        public override string ToString()
        {
            return this.Character.ToString();
        }

        public void ApplyFormat(ScreenCellFormat format)
        {
            if (format == null)
            {
                return;
            }

            this.Modifications = ScreenCellModifications.None;
            if (format.BoldMode)
            {
                this.Modifications |= ScreenCellModifications.Bold;
            }

            if (format.UnderlineMode)
            {
                this.Modifications |= ScreenCellModifications.Underline;
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

        public ScreenCell Clone()
        {
            return new ScreenCell()
            {
                Character = this.Character,
                Modifications = this.Modifications,
                ForegroundColor = this.ForegroundColor,
                BackgroundColor = this.BackgroundColor,
            };
        }

        public char Character { get; set; }
        public ScreenCellModifications Modifications { get; set; }
        public ScreenColor ForegroundColor { get; set; }
        public ScreenColor BackgroundColor { get; set; }
    }
}
