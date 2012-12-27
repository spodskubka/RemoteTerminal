using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Linq;
using System.Collections;

namespace RemoteTerminal.Terminals
{
    public sealed partial class DrawingTerminalLine : IReadOnlyList<DrawingTerminalCell>
    {
        private readonly DrawingTerminalDisplay display;
        private readonly List<DrawingTerminalCell> cells = new List<DrawingTerminalCell>();

        public DrawingTerminalLine(DrawingTerminalDisplay display)
        {
            this.display = display;

            this.Reset();
        }

        private void Reset()
        {
            this.cells.Clear();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(this.cells.Count);
            foreach (var cell in this.cells)
            {
                sb.Append(cell.ToString());
            }

            return sb.ToString();
        }

        public IList<DrawingTerminalCell> Cells
        {
            get
            {
                return this.cells;
            }
        }

        public DrawingTerminalCell this[int index]
        {
            get
            {
                //if (index >= this.display.ColumnCount)
                //{
                //    throw new ArgumentOutOfRangeException("index");
                //}

                while (this.cells.Count <= index)
                {
                    this.cells.Add(new DrawingTerminalCell(this.display));
                }

                return this.cells[index];
            }
        }

        public int Count
        {
            get { return this.cells.Count; }
        }

        public IEnumerator<DrawingTerminalCell> GetEnumerator()
        {
            return this.cells.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.cells.GetEnumerator();
        }
    }
}
