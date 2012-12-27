using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteTerminal.Connections;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// Represents a terminal.
    /// </summary>
    public interface ITerminal : IDisposable
    {
        /// <summary>
        /// Gets the name of the Terminal (e.g. dumb, vt100, xterm).
        /// </summary>
        string TerminalName { get; }

        /// <summary>
        /// Gets a value indicating whether the terminal is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Connects the terminal with the specified <see cref="IConnection"/>.
        /// </summary>
        /// <param name="connection">The <see cref="IConnection"/> to which the terminal should be connected.</param>
        void Connect(IConnection connection);

        /// <summary>
        /// Disconnects the terminal from its stream.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Writes a line of text (the terminal must not be connected).
        /// </summary>
        /// <param name="text">The text to write.</param>
        Task WriteLineAsync(string text);

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
