namespace RemoteTerminal.Screens
{
    /// <summary>
    /// Interface for a renderable screen.
    /// </summary>
    public interface IRenderableScreen
    {
        /// <summary>
        /// Gets a value indicating whether the screen was changed after the last call to <see cref="GetScreenCopy()"/>.
        /// </summary>
        bool Changed { get; }

        /// <summary>
        /// Gets a screen copy representing the current state of the screen.
        /// </summary>
        /// <returns>A screen copy representing the current state of the screen.</returns>
        IRenderableScreenCopy GetScreenCopy();

        /// <summary>
        /// Gets the number of rows.
        /// </summary>
        int RowCount { get; }

        /// <summary>
        /// Gets the number of columns.
        /// </summary>
        int ColumnCount { get; }

        /// <summary>
        /// Gets the number of rows in the scrollback buffer.
        /// </summary>
        int ScrollbackRowCount { get; }

        /// <summary>
        /// Gets or sets the scrollback position.
        /// </summary>
        int ScrollbackPosition { get; set; }
    }
}
