using System;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// Possible key modifiers.
    /// </summary>
    [Flags]
    public enum KeyModifiers
    {
        /// <summary>
        /// No key modifier.
        /// </summary>
        None = 0,

        /// <summary>
        /// The Shift key.
        /// </summary>
        Shift = 1,

        /// <summary>
        /// The Ctrl key.
        /// </summary>
        Ctrl = 1 << 1,

        /// <summary>
        /// The Alt key.
        /// </summary>
        Alt = 1 << 2,
    }
}
