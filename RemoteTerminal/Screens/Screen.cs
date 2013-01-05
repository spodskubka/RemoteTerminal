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
    public class Screen : List<ScreenLine>, IWritableScreen, IRenderableScreen
    {
        private class ScreenModifier : IScreenModifier
        {
            private readonly Screen screen;
            private bool changed;

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
                            throw new ArgumentOutOfRangeException();
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
                    var line = this.screen[this.screen.CursorRow];
                    var cell = line[this.screen.CursorColumn];
                    return cell.Character;
                }

                set
                {
                    var line = this.screen[this.screen.CursorRow];
                    var cell = line[this.screen.CursorColumn];
                    if (cell.Character != value)
                    {
                        cell.Character = value;
                        this.changed = true;
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
                var line = this.screen[this.screen.CursorRow];
                var cell = line[this.screen.CursorColumn];
                cell.ApplyFormat(format);
            }

            public void Erase(int startRow, int startColumn, int endRow, int endColumn, ScreenCellFormat format)
            {
                Screen display = this.screen;
                ScreenLine line;
                ScreenCell cell;
                for (int row = startRow; row <= endRow; row++)
                {
                    line = display[row];
                    int startColumnLine = row == startRow ? startColumn : 0;
                    int endColumnLine = row == endRow ? endColumn : this.screen.ColumnCount - 1;
                    for (int column = startColumnLine; column <= endColumnLine; column++)
                    {
                        cell = line[column];

                        cell.Character = ' ';
                        cell.ApplyFormat(format);
                    }
                }
            }

            public void CursorRowIncreaseWithScroll(int? scrollTop, int? scrollBottom)
            {
                if (this.CursorRow + 1 > (scrollBottom ?? (this.screen.Count - 1)))
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
                    this.screen.RemoveAt(scrollBottom ?? (this.screen.Count - 1));
                    this.screen.Insert(scrollTop ?? 0, new ScreenLine(this.screen.ColumnCount));
                }
            }

            public void ScrollUp(int lines, int? scrollTop, int? scrollBottom)
            {
                for (int i = 0; i < lines; i++)
                {
                    this.screen.RemoveAt(scrollTop ?? 0);
                    this.screen.Insert(scrollBottom ?? this.screen.Count, new ScreenLine(this.screen.ColumnCount));
                }
            }

            public void InsertLines(int lines, int? scrollTop, int? scrollBottom)
            {
                for (int i = 0; i < lines; i++)
                {
                    this.screen.RemoveAt(scrollBottom ?? (this.screen.Count - 1));
                    this.screen.Insert(this.screen.CursorRow, new ScreenLine(this.screen.ColumnCount));
                }
            }

            public void DeleteLines(int lines, int? scrollTop, int? scrollBottom)
            {
                for (int i = 0; i < lines; i++)
                {
                    this.screen.RemoveAt(this.screen.CursorRow);
                    this.screen.Insert(scrollBottom ?? (this.screen.Count - 1), new ScreenLine(this.screen.ColumnCount));
                }
            }

            public void InsertCells(int cells)
            {
                var line = this.screen[this.screen.CursorRow];
                for (int i = 0; i < cells; i++)
                {
                    line.RemoveAt(line.Count - 1);
                    line.Insert(this.screen.CursorColumn, new ScreenCell());
                }
            }

            public void DeleteCells(int cells)
            {
                var line = this.screen[this.screen.CursorRow];
                for (int i = 0; i < cells; i++)
                {
                    line.RemoveAt(this.screen.CursorColumn);
                    line.Insert(line.Count, new ScreenCell());
                }
            }

            public void Resize(int rows, int columns)
            {
                while (this.screen.Count > rows)
                {
                    this.screen.RemoveAt(0);
                }

                while (this.screen.Count < rows)
                {
                    this.screen.Add(new ScreenLine(columns));
                }

                foreach (var line in this.screen)
                {
                    while (line.Count > columns)
                    {
                        line.RemoveAt(line.Count - 1);
                    }

                    while (line.Count < columns)
                    {
                        line.Add(new ScreenCell());
                    }
                }
            }

            public void Dispose()
            {
                if (this.changed)
                {
                    this.screen.Changed = true;
                }

                Monitor.Exit(this.screen.changeLock);
            }
        }

        private class TerminalScreenCopy : IRenderableScreenCopy
        {
            private readonly ScreenCell[][] cells;

            public TerminalScreenCopy(Screen terminalScreen)
            {
                this.cells = terminalScreen.Select(l => l.Select(c => c.Clone()).ToArray()).ToArray();
                this.CursorRow = terminalScreen.CursorRow;
                this.CursorColumn = terminalScreen.CursorColumn;
                this.CursorHidden = terminalScreen.CursorHidden;
                this.HasFocus = terminalScreen.HasFocus;
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

        private readonly object changeLock = new object();

        public Screen(int rows, int columns)
            : base(rows)
        {
            this.CursorColumn = 0;
            this.CursorRow = 0;
            this.CursorHidden = false;
            for (int i = 0; i < rows; i++)
            {
                this.Add(new ScreenLine(columns));
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
                return this.Count;
            }
        }

        /// <summary>
        /// Gets the amount of columns this display can show.
        /// </summary>
        public int ColumnCount
        {
            get
            {
                var line = this[0];
                return line.Count;
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
