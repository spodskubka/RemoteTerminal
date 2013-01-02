using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTerminal.Terminals
{
    public interface IConnectionInitializingTerminal
    {
        /// <summary>
        /// Gets the name of the Terminal (e.g. dumb, vt100, xterm).
        /// </summary>
        string TerminalName { get; }

        /// <summary>
        /// Writes a line of text (the terminal must not be connected).
        /// </summary>
        /// <param name="text">The text to write.</param>
        void WriteLine(string text);

        /// <summary>
        /// Reads a line of text (the terminal must not be connected).
        /// </summary>
        /// <param name="prompt">The prompt.</param>
        /// <param name="echo">A value indicating whether the input should be echoed on the terminal.</param>
        /// <returns>The read line.</returns>
        Task<string> ReadLineAsync(string prompt, bool echo);

        /// <summary>
        /// Gets the amount of columns of the terminal.
        /// </summary>
        int Columns { get; }

        /// <summary>
        /// Gets the rows of the terminal.
        /// </summary>
        int Rows { get; }
    }
}
