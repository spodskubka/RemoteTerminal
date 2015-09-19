namespace RemoteTerminal.Screens
{
    /// <summary>
    /// Interface for a renderable screen cell.
    /// </summary>
    public interface IRenderableScreenCell
    {
        /// <summary>
        /// Gets the contained character.
        /// </summary>
        char Character { get; }

        /// <summary>
        /// Gets the modifications.
        /// </summary>
        ScreenCellModifications Modifications { get; }

        /// <summary>
        /// Gets the foreground color.
        /// </summary>
        ScreenColor ForegroundColor { get; }

        /// <summary>
        /// Gets the background color.
        /// </summary>
        ScreenColor BackgroundColor { get; }
    }
}
