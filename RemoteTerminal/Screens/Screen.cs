using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RemoteTerminal.Screens
{
    /// <summary>
    /// The virtual in-memory representation of a terminal screen.
    /// </summary>
    public class Screen : IWritableScreen, IRenderableScreen
    {
        /// <summary>
        /// The screen modifier for the <see cref="Screen"/> class.
        /// </summary>
        private class ScreenModifier : IScreenModifier
        {
            /// <summary>
            /// The screen that can be modified with this screen modifier.
            /// </summary>
            private readonly Screen screen;

            /// <summary>
            /// A value indicating whether the screen was changed in any way (cursor position, content, etc.).
            /// </summary>
            private bool changed;

            /// <summary>
            /// A value indicating whether the content of the screen was changed.
            /// </summary>
            /// <remarks>
            /// This is used to control scrollback resets when screen content changes.
            /// </remarks>
            private bool contentChanged;

            /// <summary>
            /// Initializes a new instance of the <see cref="ScreenModifier"/> class.
            /// </summary>
            /// <param name="screen">The screen that can be modified with this screen modifier.</param>
            public ScreenModifier(Screen screen)
            {
                this.screen = screen;
                Monitor.Enter(screen.changeLock);
            }

            /// <summary>
            /// Gets or sets the row position of the cursor (zero-based).
            /// </summary>
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

            /// <summary>
            /// Gets or sets the column position of the cursor (zero-based).
            /// </summary>
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
                            return;
                        }

                        this.screen.CursorColumn = value;
                        this.changed = true;
                    }
                }
            }

            /// <summary>
            /// Gets or sets the character at the cursor position.
            /// </summary>
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

            /// <summary>
            /// Gets or sets a value indicating whether the screen has focus.
            /// </summary>
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

            /// <summary>
            /// Applies the specified format to the cursor position.
            /// </summary>
            /// <param name="format">The format to apply.</param>
            public void ApplyFormatToCursor(ScreenCellFormat format)
            {
                var line = this.screen.CurrentBuffer[this.screen.CursorRow];
                var cell = line[this.screen.CursorColumn];
                cell.ApplyFormat(format);
                this.changed = true;
                this.contentChanged = true;
            }

            /// <summary>
            /// Erases the specified area of the screen (fills it with blanks/space chars) and applies the specified format to each cell in the erased area.
            /// </summary>
            /// <param name="startRow">The start row (zero-based).</param>
            /// <param name="startColumn">The start column (zero-based).</param>
            /// <param name="endRow">The end row (zero-based), larger or equal to <paramref name="startRow"/>.</param>
            /// <param name="endColumn">The end column (zero-based), larger or equal to <paramref name="startColumn"/>.</param>
            /// <param name="format">The format applied to the erased area.</param>
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

            /// <summary>
            /// Moves the cursor one row down or scrolls the terminal contents up if at the bottom of the screen/scrolling area.
            /// </summary>
            /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
            /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
            /// <remarks>
            /// Adds rows to the scrollback buffer that get scrolled out at the top of the screen.
            /// </remarks>
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

            /// <summary>
            /// Moves the cursor one row up or scrolls the terminal contents down if at the top of the screen/scrolling area.
            /// </summary>
            /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
            /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
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

            /// <summary>
            /// Scrolls the screen/scrolling area down the specified amount of lines.
            /// </summary>
            /// <param name="lines">The amount of lines to scroll.</param>
            /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
            /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
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

            /// <summary>
            /// Scrolls the screen/scrolling area up the specified amount of lines.
            /// </summary>
            /// <param name="lines">The amount of lines to scroll.</param>
            /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
            /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
            /// <remarks>
            /// Adds rows to the scrollback buffer that get scrolled out at the top of the screen.
            /// </remarks>
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

            /// <summary>
            /// Inserts the specified amount of blank lines at the current row position, scrolling down the following lines inside the screen/scrolling area.
            /// </summary>
            /// <param name="lines">The amount of lines to insert.</param>
            /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
            /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
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

            /// <summary>
            /// Deletes the specified amount of lines at the current row position, scrolling up the following lines inside the screen/scrolling area and adding blank lines at the bottom.
            /// </summary>
            /// <param name="lines">The amount of lines to insert.</param>
            /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
            /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
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

            /// <summary>
            /// Inserts the specified amount of blank cells at the current column position, moving right the following cells (cells that are moved out at the right of the screen are discarded).
            /// </summary>
            /// <param name="cells">The amount of cells to insert.</param>
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

            /// <summary>
            /// Deletes the specified amount of cells at the current column position, moving left the following cells (filling up with blank cells at the right of the screen).
            /// </summary>
            /// <param name="cells">The amount of cells to insert.</param>
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

            /// <summary>
            /// Resizes the screen to the specified size.
            /// </summary>
            /// <param name="rows">The new amount of rows.</param>
            /// <param name="columns">The new amount of columns.</param>
            public void Resize(int rows, int columns)
            {
                this.Resize(this.screen.mainBuffer, rows, columns);
                this.Resize(this.screen.alternateBuffer, rows, columns);
            }

            /// <summary>
            /// Switches the active screen buffer either to the main buffer or to the alternate buffer.
            /// </summary>
            /// <param name="alternateBuffer">A value indicating whether to switch to the alternate buffer; false switches to the main buffer.</param>
            public void SwitchBuffer(bool alternateBuffer)
            {
                if (this.screen.useAlternateBuffer != alternateBuffer)
                {
                    this.screen.useAlternateBuffer = alternateBuffer;
                    this.changed = true;
                    this.contentChanged = true;
                }
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
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

            /// <summary>
            /// Resizes the specified screen buffer to the specified size.
            /// </summary>
            /// <param name="rows">The new amount of rows.</param>
            /// <param name="columns">The new amount of columns.</param>
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

        /// <summary>
        /// The screen copy for the <see cref="Screen"/> class.
        /// </summary>
        private class TerminalScreenCopy : IRenderableScreenCopy
        {
            // TODO: this class should be called "ScreenCopy"...

            /// <summary>
            /// The screen cells that the terminal screen is composed of.
            /// </summary>
            private readonly ScreenCell[][] cells;

            /// <summary>
            /// Initializes a new instance of the <see cref="TerminalScreenCopy"/> class.
            /// </summary>
            /// <param name="terminalScreen">The screen from which to copy the content.</param>
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

            /// <summary>
            /// Gets the row position of the cursor (zero-based).
            /// </summary>
            public int CursorRow
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the column position of the cursor (zero-based).
            /// </summary>
            public int CursorColumn
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets a value indicating whether the cursor should be hidden.
            /// </summary>
            public bool CursorHidden
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets a value indicating whether the screen has focus.
            /// </summary>
            public bool HasFocus
            {
                get;
                private set;
            }

            /// <summary>
            /// Gets the cells that the terminal screen is composed of.
            /// </summary>
            public IRenderableScreenCell[][] Cells
            {
                get { return this.cells; }
            }
        }

        /// <summary>
        /// The scrollback buffer.
        /// </summary>
        private readonly ScreenScrollbackBuffer scrollbackBuffer = new ScreenScrollbackBuffer(1000);

        /// <summary>
        /// The main screen buffer.
        /// </summary>
        private readonly List<ScreenLine> mainBuffer;

        /// <summary>
        /// The alternate screen buffer.
        /// </summary>
        private readonly List<ScreenLine> alternateBuffer;

        /// <summary>
        /// A value indicating whether the alternate buffer is currently active; false indicates that the main buffer is active.
        /// </summary>
        private bool useAlternateBuffer = false;

        /// <summary>
        /// A lock object for changes to this screen.
        /// </summary>
        /// <remarks>
        /// This lock must be used when modifying the screen and when copying it (to prevent race conditions when copying the screen during a modification).</remarks>
        private readonly object changeLock = new object();

        /// <summary>
        /// The current scrollback position.
        /// </summary>
        private int scrollbackPosition = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Screen"/> class with the specified screen size.
        /// </summary>
        /// <param name="rows">The amount of rows on the screen.</param>
        /// <param name="columns">The amount of columns on the screen.</param>
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

        /// <summary>
        /// Gets the current screen buffer (main or alternate).
        /// </summary>
        private List<ScreenLine> CurrentBuffer
        {
            get
            {
                return useAlternateBuffer ? this.alternateBuffer : this.mainBuffer;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the screen has changed since the previous call to <see cref="GetScreenCopy()"/>.
        /// </summary>
        public bool Changed { get; private set; }

        /// <summary>
        /// Gets the change lock for this screen.
        /// </summary>
        // TODO: this is not needed and should probably be removed...
        public object ChangeLock
        {
            get
            {
                return this.changeLock;
            }
        }

        /// <summary>
        /// Gets the number of rows.
        /// </summary>
        public int RowCount
        {
            get
            {
                return this.mainBuffer.Count;
            }
        }

        /// <summary>
        /// Gets the number of columns.
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
        /// Gets the number of rows in the scrollback buffer.
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

        /// <summary>
        /// Gets a screen copy.
        /// </summary>
        /// <returns></returns>
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
