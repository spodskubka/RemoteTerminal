using System;

namespace RemoteTerminal.Screens
{
    /// <summary>
    /// The modifications to apply to the display of screen cell text.
    /// </summary>
    [Flags]
    public enum ScreenCellModifications
    {
        /// <summary>
        /// No modifications.
        /// </summary>
        None = 0,

        /// <summary>
        /// Bold text.
        /// </summary>
        Bold = 1,

        /// <summary>
        /// Underlined text.
        /// </summary>
        Underline = 1 << 1,
    }
}
