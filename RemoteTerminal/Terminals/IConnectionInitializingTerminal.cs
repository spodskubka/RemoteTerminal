// Remote Terminal, an SSH/Telnet terminal emulator for Microsoft Windows
// Copyright (C) 2012-2015 Stefan Podskubka
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Threading.Tasks;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// Interface for a connection initializing terminal.
    /// </summary>
    public interface IConnectionInitializingTerminal
    {
        /// <summary>
        /// Gets the name of the terminal implementation (e.g. dumb, vt100, xterm).
        /// </summary>
        /// <remarks>
        /// In case of an SSH connection this may be sent to the SSH server
        /// </remarks>
        string TerminalName { get; }

        /// <summary>
        /// Writes a line of text to the terminal while no connection is established (e.g. for connection initialization or disconnect notification).
        /// </summary>
        /// <param name="text">The line of text to write to the terminal.</param>
        /// <exception cref="InvalidOperationException">A connection is established at the moment.</exception>
        void WriteLine(string text);

        /// <summary>
        /// Reads a line of user input while no connection is established (e.g. for connection initialization).
        /// </summary>
        /// <param name="prompt">A prompt to display (<see cref="string.Empty"/> for no prompt).</param>
        /// <param name="echo">A value indicating whether to echo the user input back to the terminal in plain text (<see cref="false"/> for password input with * characters).</param>
        /// <returns>The read line of user input (without a new-line at the end).</returns>
        /// <exception cref="InvalidOperationException">A connection is established at the moment.</exception>
        Task<string> ReadLineAsync(string prompt, bool echo);

        /// <summary>
        /// Gets the amount of columns on the terminal screen.
        /// </summary>
        int Columns { get; }

        /// <summary>
        /// Gets the amount of rows on the terminal screen.
        /// </summary>
        int Rows { get; }
    }
}
