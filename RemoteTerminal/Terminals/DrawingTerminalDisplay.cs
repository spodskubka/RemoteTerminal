using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace RemoteTerminal.Terminals
{
    public sealed partial class DrawingTerminalDisplay :IReadOnlyList<DrawingTerminalLine>
    {
        private readonly List<DrawingTerminalLine> lines = new List<DrawingTerminalLine>();
        public static readonly DrawingTerminalCellFormat CursorFormat = new DrawingTerminalCellFormat()
        {
            ForegroundColor = Colors.Black,
            BackgroundColor = Colors.Green,
        };

        public DrawingTerminalDisplay()
        {
            this.ChangeLock = new object();

            this.CursorColumn = 0;
            this.CursorRow = 0;
            this.CursorHidden = false;
            this.Reset();
        }

        private void Reset()
        {
            //    this.CursorRow = 0;
            //    this.CursorColumn = 0;

            lock (this.ChangeLock)
            {
                this.lines.Clear();
                this.Changed = true;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(this.lines.Count * (this.ColumnCount + 2));
            foreach (var line in this.lines)
            {
                sb.Append(line.ToString() + Environment.NewLine);
            }

            return sb.ToString();
        }

        public IList<DrawingTerminalLine> Lines
        {
            get
            {
                return this.lines;
            }
        }

        public DrawingTerminalLine this[int index]
        {
            get
            {
                //    if (index >= this.RowCount)
                //    {
                //        throw new ArgumentOutOfRangeException("index");
                //    }

                while (this.lines.Count <= index)
                {
                    this.lines.Add(new DrawingTerminalLine(this));
                }

                return this.lines[index];
            }
        }

        public object ChangeLock { get; set; }

        public bool Changed { get; set; }

        /// <summary>
        /// Gets the amount of rows this display can show.
        /// </summary>
        public int RowCount { get; private set; }

        /// <summary>
        /// Gets the amount of columns this display can show;
        /// </summary>
        public int ColumnCount { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the display has input focus.
        /// </summary>
        public bool HasFocus { get; set; }

        /// <summary>
        /// Gets the row position of the cursor (zero-based).
        /// </summary>
        public int CursorRow { get; set; }

        /// <summary>
        /// Gets the column position of the cursor (zero-based).
        /// </summary>
        public int CursorColumn { get; set; }

        /// <summary>
        /// Gets a value indicating whether the cursor should be hidden.
        /// </summary>
        public bool CursorHidden { get; set; }

        ///// <summary>
        ///// Gets the top line of the scroll area (zero-based). If null then it's the top-most line.
        ///// </summary>
        //private int? ScrollTopRow { get; set; }

        ///// <summary>
        ///// Gets the bottom line of the scroll area (zero-based). null means the bottom line.
        ///// </summary>
        //private int? ScrollBottomRow { get; set; }

        //public void Erase(int startRow, int startColumn, int endRow, int endColumn)
        //{
        //    if (startRow < 0 || startRow >= this.RowCount)
        //    {
        //        throw new ArgumentOutOfRangeException("startRow");
        //    }

        //    if (startColumn < 0 || startColumn >= this.ColumnCount)
        //    {
        //        throw new ArgumentOutOfRangeException("startRow");
        //    }

        //    if (endRow < 0 || endRow >= this.RowCount)
        //    {
        //        throw new ArgumentOutOfRangeException("endRow");
        //    }

        //    if (endColumn < 0 || endColumn >= this.ColumnCount)
        //    {
        //        throw new ArgumentOutOfRangeException("endRow");
        //    }

        //    if (startRow > endRow)
        //    {
        //        throw new ArgumentOutOfRangeException("endRow");
        //    }

        //    DrawingTerminalLine line;
        //    DrawingTerminalCell cell;
        //    for (int row = startRow; row <= endRow; row++)
        //    {
        //        if (this.Display.Lines.Count <= row)
        //        {
        //            break;
        //        }

        //        line = this.lines[row];
        //        for (int column = (row == startRow ? startColumn : 0); column <= (row == endRow ? endColumn : this.ColumnCount); column++)
        //        {
        //            if (line.Cells.Count <= column)
        //            {
        //                break;
        //            }

        //            cell = line.Cells[column];

        //            cell.Reset();
        //            //cell.Character = ' ';
        //            //cell.ApplyFormat(this.currentFormat);
        //        }
        //    }
        //}

        public int Count
        {
            get { return this.lines.Count; }
        }

        public IEnumerator<DrawingTerminalLine> GetEnumerator()
        {
            return this.lines.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.lines.GetEnumerator();
        }
    }
}
