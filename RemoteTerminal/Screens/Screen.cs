using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using System.Threading;

namespace RemoteTerminal.Screens
{
    public class Screen : IWritableScreen, IRenderableScreen
    {
        private class ScreenModifier : IScreenModifier
        {
            private readonly Screen screen;
            private bool changed;
            private bool contentChanged;

            public ScreenModifier(Screen screen)
            {
                this.screen = screen;
                Monitor.Enter(screen.changeLock);
            }

            public int CursorRow
            {
                get
                {
                    return this.screen.CursorRow;
                }

                set
                {
                    if (value != this.screen.CursorRow)
                    {
                        if (value >= this.screen.RowCount)
                        {
                            return;
                        }

                        this.screen.CursorRow = value;
                        this.changed = true;
                    }
                }
            }

            public int CursorColumn
            {
                get
                {
                    return this.screen.CursorColumn;
                }

                set
                {
                    if (value != this.screen.CursorColumn)
                    {
                        if (value >= this.screen.ColumnCount)
                        {
                            throw new ArgumentOutOfRangeException();
                        }

                        this.screen.CursorColumn = value;
                        this.changed = true;
                    }
                }
            }

            public char CursorCharacter
            {
                get
                {
                    var line = this.screen.CurrentBuffer[this.screen.CursorRow];
                    var cell = line[this.screen.CursorColumn];
                    return cell.Character;
                }

                set
                {
                    var line = this.screen.CurrentBuffer[this.screen.CursorRow];
                    var cell = line[this.screen.CursorColumn];
                    if (cell.Character != value)
                    {
                        cell.Character = value;
                        this.changed = true;
                        this.contentChanged = true;
                    }
                }
            }

            public bool HasFocus
            {
                get
                {
                    return this.screen.HasFocus;
                }

                set
                {
                    if (this.screen.HasFocus != value)
                    {
                        this.screen.HasFocus = value;
                        this.changed = true;
                    }
                }
            }

            public void ApplyFormatToCursor(ScreenCellFormat format)
            {
                var line = this.screen.CurrentBuffer[this.screen.CursorRow];
                var cell = line[this.screen.CursorColumn];
                cell.ApplyFormat(format);
                this.changed = true;
                this.contentChanged = true;
            }

            public void Erase(int startRow, int startColumn, int endRow, int endColumn, ScreenCellFormat format)
            {
                Screen display = this.screen;
                ScreenLine line;
                ScreenCell cell;
                for (int row = startRow; row <= endRow; row++)
                {
                    line = display.CurrentBuffer[row];
                    int startColumnLine = row == startRow ? startColumn : 0;
                    int endColumnLine = row == endRow ? endColumn : this.screen.ColumnCount - 1;
                    for (int column = startColumnLine; column <= endColumnLine; column++)
                    {
                        cell = line[column];

                        cell.Character = ' ';
                        cell.ApplyFormat(format);
                        this.changed = true;
                        this.contentChanged = true;
                    }
                }
            }

            public void CursorRowIncreaseWithScroll(int? scrollTop, int? scrollBottom)
            {
                if (this.CursorRow + 1 > (scrollBottom ?? (this.screen.CurrentBuffer.Count - 1)))
                {
                    this.ScrollUp(1, scrollTop, scrollBottom);
                }
                else
                {
                    this.CursorRow++;
                }
            }

            public void CursorRowDecreaseWithScroll(int? scrollTop, int? scrollBottom)
            {
                if (this.CursorRow - 1 < (scrollTop ?? 0))
                {
                    this.ScrollDown(1, scrollTop, scrollBottom);
                }
                else
                {
                    this.CursorRow--;
                }
            }

            public void ScrollDown(int lines, int? scrollTop, int? scrollBottom)
            {
                for (int i = 0; i < lines; i++)
                {
                    this.screen.CurrentBuffer.RemoveAt(scrollBottom ?? (this.screen.CurrentBuffer.Count - 1));
                    this.screen.CurrentBuffer.Insert(scrollTop ?? 0, new ScreenLine(this.screen.ColumnCount));
                    this.changed = true;
                    this.contentChanged = true;
                }
            }

            public void ScrollUp(int lines, int? scrollTop, int? scrollBottom)
            {
                ScreenLine newLine;
                for (int i = 0; i < lines; i++)
                {
                    if (!this.screen.useAlternateBuffer && (scrollTop ?? 0) == 0)
                    {
                        this.screen.scrollbackBuffer.Append(this.screen.mainBuffer[0]);
                    }

                    this.screen.CurrentBuffer.RemoveAt(scrollTop ?? 0);
                    newLine = new ScreenLine(this.screen.ColumnCount);
                    this.screen.CurrentBuffer.Insert(scrollBottom ?? this.screen.CurrentBuffer.Count, newLine);
                    this.changed = true;
                    this.contentChanged = true;
                }
            }

            public void InsertLines(int lines, int? scrollTop, int? scrollBottom)
            {
                for (int i = 0; i < lines; i++)
                {
                    this.screen.CurrentBuffer.RemoveAt(scrollBottom ?? (this.screen.CurrentBuffer.Count - 1));
                    this.screen.CurrentBuffer.Insert(this.screen.CursorRow, new ScreenLine(this.screen.ColumnCount));
                    this.changed = true;
                    this.contentChanged = true;
                }
            }

            public void DeleteLines(int lines, int? scrollTop, int? scrollBottom)
            {
                for (int i = 0; i < lines; i++)
                {
                    this.screen.CurrentBuffer.RemoveAt(this.screen.CursorRow);
                    this.screen.CurrentBuffer.Insert(scrollBottom ?? (this.screen.CurrentBuffer.Count - 1), new ScreenLine(this.screen.ColumnCount));
                    this.changed = true;
                    this.contentChanged = true;
                }
            }

            public void InsertCells(int cells)
            {
                var line = this.screen.CurrentBuffer[this.screen.CursorRow];
                cells = Math.Min(cells, line.Count - this.screen.CursorColumn);

                var movedCells = line.GetRange(line.Count - cells, cells);
                line.RemoveRange(line.Count - cells, cells);
                foreach (var movedCell in movedCells)
                {
                    movedCell.Reset();
                }
                line.InsertRange(this.screen.CursorColumn, movedCells);

                this.changed = true;
                this.contentChanged = true;
            }

            public void DeleteCells(int cells)
            {
                var line = this.screen.CurrentBuffer[this.screen.CursorRow];
                cells = Math.Min(cells, line.Count - this.screen.CursorColumn);

                var movedCells = line.GetRange(this.screen.CursorColumn, cells);
                line.RemoveRange(this.screen.CursorColumn, cells);
                foreach (var movedCell in movedCells)
                {
                    movedCell.Reset();
                }
                line.AddRange(movedCells);

                this.changed = true;
                this.contentChanged = true;
            }

            public void Resize(int rows, int columns)
            {
                this.Resize(this.screen.mainBuffer, rows, columns);
                this.Resize(this.screen.alternateBuffer, rows, columns);
            }

            public void SwitchBuffer(bool alternateBuffer)
            {
                if (this.screen.useAlternateBuffer != alternateBuffer)
                {
                    this.screen.useAlternateBuffer = alternateBuffer;
                    this.changed = true;
                    this.contentChanged = true;
                }
            }

            public void Dispose()
            {
                if (this.changed)
                {
                    this.screen.Changed = true;
                }

                if (this.contentChanged)
                {
                    this.screen.scrollbackPosition = 0;
                }

                Monitor.Exit(this.screen.changeLock);
            }

            private void Resize(List<ScreenLine> buffer, int rows, int columns)
            {
                while (buffer.Count > rows)
                {
                    if (this.CursorRow < buffer.Count - 1)
                    {
                        buffer.RemoveAt(buffer.Count - 1);
                    }
                    else
                    {
                        this.CursorRow--;
                        buffer.RemoveAt(0);
                    }
                }

                while (buffer.Count < rows)
                {
                    buffer.Add(new ScreenLine(columns));
                }

                foreach (var line in buffer)
                {
                    if (line.Count > columns)
                    {
                        var cells = line.GetRange(line.Count - (line.Count - columns), line.Count - columns);
                        line.RemoveRange(line.Count - (line.Count - columns), line.Count - columns);
                        ScreenCell.RecycleCells(cells);
                    }
                    else if (line.Count < columns)
                    {
                        line.AddRange(ScreenCell.GetFreshCells(columns - line.Count));
                    }
                }

                this.CursorRow = Math.Min(this.CursorRow, rows - 1);
                this.CursorColumn = Math.Min(this.CursorColumn, columns - 1);

                this.changed = true;
            }
        }

        private class TerminalScreenCopy : IRenderableScreenCopy
        {
            private readonly ScreenCell[][] cells;

            public TerminalScreenCopy(Screen terminalScreen)
            {
                this.CursorRow = terminalScreen.CursorRow;
                this.CursorColumn = terminalScreen.CursorColumn;
                this.CursorHidden = terminalScreen.CursorHidden;
                this.HasFocus = terminalScreen.HasFocus;

                IEnumerable<ScreenLine> lines;
                if (!terminalScreen.useAlternateBuffer && terminalScreen.scrollbackPosition > 0)
                {
                    lines = terminalScreen.scrollbackBuffer.Skip(terminalScreen.scrollbackBuffer.Count - terminalScreen.scrollbackPosition).Take(terminalScreen.mainBuffer.Count);
                    if (terminalScreen.scrollbackPosition < terminalScreen.mainBuffer.Count)
                    {
                        lines = lines.Union(terminalScreen.mainBuffer.Take(terminalScreen.mainBuffer.Count - terminalScreen.scrollbackPosition));
                    }

                    this.CursorRow += terminalScreen.scrollbackPosition;
                }
                else
                {
                    lines = terminalScreen.CurrentBuffer;
                }

                this.cells = lines.Select(l => l.Select(c => c.Clone()).ToArray()).ToArray();
            }

            public int CursorRow
            {
                get;
                private set;
            }

            public int CursorColumn
            {
                get;
                private set;
            }

            public bool CursorHidden
            {
                get;
                private set;
            }

            public bool HasFocus
            {
                get;
                private set;
            }

            public IRenderableScreenCell[][] Cells
            {
                get { return this.cells; }
            }
        }

        private readonly ScreenScrollbackBuffer scrollbackBuffer = new ScreenScrollbackBuffer(1000);
        private readonly List<ScreenLine> mainBuffer;
        private readonly List<ScreenLine> alternateBuffer;
        private bool useAlternateBuffer = false;
        private readonly object changeLock = new object();
        private int scrollbackPosition = 0;

        public Screen(int rows, int columns)
        {
            this.CursorColumn = 0;
            this.CursorRow = 0;
            this.CursorHidden = false;

            this.mainBuffer = new List<ScreenLine>(rows);
            for (int i = 0; i < rows; i++)
            {
                this.mainBuffer.Add(new ScreenLine(columns));
            }

            this.alternateBuffer = new List<ScreenLine>(rows);
            for (int i = 0; i < rows; i++)
            {
                this.alternateBuffer.Add(new ScreenLine(columns));
            }

            this.ScrollbackPosition = 0;
        }

        private List<ScreenLine> CurrentBuffer
        {
            get
            {
                return useAlternateBuffer ? this.alternateBuffer : this.mainBuffer;
            }
        }

        public bool Changed { get; private set; }

        public object ChangeLock
        {
            get
            {
                return this.changeLock;
            }
        }

        /// <summary>
        /// Gets the amount of rows this display can show.
        /// </summary>
        public int RowCount
        {
            get
            {
                return this.mainBuffer.Count;
            }
        }

        /// <summary>
        /// Gets the amount of columns this display can show.
        /// </summary>
        public int ColumnCount
        {
            get
            {
                var line = this.mainBuffer[0];
                return line.Count;
            }
        }

        /// <summary>
        /// Gets the amount of rows in the scrollback buffer.
        /// </summary>
        public int ScrollbackRowCount
        {
            get
            {
                return this.scrollbackBuffer.Count;
            }
        }

        /// <summary>
        /// Gets or sets the scrollback position.
        /// </summary>
        public int ScrollbackPosition
        {
            get
            {
                return this.scrollbackPosition;
            }

            set
            {
                if (value > this.scrollbackBuffer.Count)
                {
                    throw new ArgumentOutOfRangeException("value", value, "Scrollback buffer only contains " + this.scrollbackBuffer.Count + " rows.");
                }

                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "Scrollback position must be >= 0.");
                }

                this.scrollbackPosition = value;
                lock (this.changeLock)
                {
                    this.Changed = true;
                }
            }
        }

        /// <summary>
        /// Gets the row position of the cursor (zero-based).
        /// </summary>
        public int CursorRow { get; private set; }

        /// <summary>
        /// Gets the column position of the cursor (zero-based).
        /// </summary>
        public int CursorColumn { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the cursor should be hidden.
        /// </summary>
        public bool CursorHidden { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the screen has focus.
        /// </summary>
        private bool HasFocus { get; set; }

        public IScreenModifier GetModifier()
        {
            return new Screen.ScreenModifier(this);
        }

        public IRenderableScreenCopy GetScreenCopy()
        {
            lock (this.changeLock)
            {
                var screenCopy = new Screen.TerminalScreenCopy(this);
                this.Changed = false;
                return screenCopy;
            }
        }
    }
}
