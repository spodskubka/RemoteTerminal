namespace RemoteTerminal.Screens
{
    /// <summary>
    /// The interface for a writable screen.
    /// </summary>
    public interface IWritableScreen : IRenderableScreen
    {
        /// <summary>
        /// Gets the screen modifier.
        /// </summary>
        /// <returns>The screen modifier.</returns>
        IScreenModifier GetModifier();

        /// <summary>
        /// Gets the row position of the cursor (zero-based).
        /// </summary>
        int CursorRow { get; }

        /// <summary>
        /// Gets the column position of the cursor (zero-based).
        /// </summary>
        int CursorColumn { get; }
    }
}
