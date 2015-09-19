using System;

namespace RemoteTerminal.Screens
{
    /// <summary>
    /// The interface for a screen modifier.
    /// </summary>
    public interface IScreenModifier : IDisposable
    {
        /// <summary>
        /// Gets or sets the row position of the cursor (zero-based).
        /// </summary>
        int CursorRow { get; set; }

        /// <summary>
        /// Gets or sets the column position of the cursor (zero-based).
        /// </summary>
        int CursorColumn { get; set; }

        /// <summary>
        /// Gets or sets the character at the cursor position.
        /// </summary>
        char CursorCharacter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the screen has focus.
        /// </summary>
        bool HasFocus { get; set; }

        /// <summary>
        /// Applies the specified format to the cursor position.
        /// </summary>
        /// <param name="format">The format to apply.</param>
        void ApplyFormatToCursor(ScreenCellFormat format);

        /// <summary>
        /// Erases the specified area of the screen (fills it with blanks/space chars) and applies the specified format to each cell in the erased area.
        /// </summary>
        /// <param name="startRow">The start row (zero-based).</param>
        /// <param name="startColumn">The start column (zero-based).</param>
        /// <param name="endRow">The end row (zero-based), larger or equal to <paramref name="startRow"/>.</param>
        /// <param name="endColumn">The end column (zero-based), larger or equal to <paramref name="startColumn"/>.</param>
        /// <param name="format">The format applied to the erased area.</param>
        void Erase(int startRow, int startColumn, int endRow, int endColumn, ScreenCellFormat format);

        /// <summary>
        /// Moves the cursor one row down or scrolls the terminal contents up if at the bottom of the screen/scrolling area.
        /// </summary>
        /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
        /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
        /// <remarks>
        /// Adds rows to the scrollback buffer that get scrolled out at the top of the screen.
        /// </remarks>
        void CursorRowIncreaseWithScroll(int? scrollTop, int? scrollBottom);

        /// <summary>
        /// Moves the cursor one row up or scrolls the terminal contents down if at the top of the screen/scrolling area.
        /// </summary>
        /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
        /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
        void CursorRowDecreaseWithScroll(int? scrollTop, int? scrollBottom);

        /// <summary>
        /// Scrolls the screen/scrolling area down the specified amount of lines.
        /// </summary>
        /// <param name="lines">The amount of lines to scroll.</param>
        /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
        /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
        void ScrollDown(int lines, int? scrollTop, int? scrollBottom);

        /// <summary>
        /// Scrolls the screen/scrolling area up the specified amount of lines.
        /// </summary>
        /// <param name="lines">The amount of lines to scroll.</param>
        /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
        /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
        /// <remarks>
        /// Adds rows to the scrollback buffer that get scrolled out at the top of the screen.
        /// </remarks>
        void ScrollUp(int lines, int? scrollTop, int? scrollBottom);

        /// <summary>
        /// Inserts the specified amount of blank lines at the current row position, scrolling down the following lines inside the screen/scrolling area.
        /// </summary>
        /// <param name="lines">The amount of lines to insert.</param>
        /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
        /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
        void InsertLines(int lines, int? scrollTop, int? scrollBottom);

        /// <summary>
        /// Deletes the specified amount of lines at the current row position, scrolling up the following lines inside the screen/scrolling area and adding blank lines at the bottom.
        /// </summary>
        /// <param name="lines">The amount of lines to insert.</param>
        /// <param name="scrollTop">The top row of the scrolling area; null specifies the top of the screen.</param>
        /// <param name="scrollBottom">The bottom row of the scrolling area; null specifies the bottom of the screen.</param>
        void DeleteLines(int lines, int? scrollTop, int? scrollBottom);

        /// <summary>
        /// Inserts the specified amount of blank cells at the current column position, moving right the following cells (cells that are moved out at the right of the screen are discarded).
        /// </summary>
        /// <param name="cells">The amount of cells to insert.</param>
        void InsertCells(int cells);

        /// <summary>
        /// Deletes the specified amount of cells at the current column position, moving left the following cells (filling up with blank cells at the right of the screen).
        /// </summary>
        /// <param name="cells">The amount of cells to insert.</param>
        void DeleteCells(int cells);

        /// <summary>
        /// Resizes the screen to the specified size.
        /// </summary>
        /// <param name="rows">The new amount of rows.</param>
        /// <param name="columns">The new amount of columns.</param>
        void Resize(int rows, int columns);

        /// <summary>
        /// Switches the active screen buffer either to the main buffer or to the alternate buffer.
        /// </summary>
        /// <param name="alternateBuffer">A value indicating whether to switch to the alternate buffer; false switches to the main buffer.</param>
        void SwitchBuffer(bool alternateBuffer);
    }
}
