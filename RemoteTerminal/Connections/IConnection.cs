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

using System;
using System.Threading.Tasks;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;

namespace RemoteTerminal.Connections
{
    /// <summary>
    /// Represents a connection (more specific: a shell connection).
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the connection object is actually connected to a server.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Initializes the connection object with the specified connection data.
        /// </summary>
        /// <param name="connectionData">The connection data for the connection.</param>
        /// <exception cref="ObjectDisposedException">The connection object is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The connection object is currently connected.</exception>
        /// <exception cref="ArgumentException">The <paramref name="connectionData"/> object contains a connection type that is not supported by the connection object.</exception>
        /// <exception cref="Exception">Some other error occured.</exception>
        void Initialize(ConnectionData connectionData);

        /// <summary>
        /// Establishes the connection to the server, using the specified <paramref name="terminal"/> for connection initialization (authentication, etc.).
        /// </summary>
        /// <param name="terminal">The terminal to use for connection initialization.</param>
        /// <returns>A value indicating whether the connection was successfully established.</returns>
        /// <exception cref="ObjectDisposedException">The connection object is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The connection object is currently connected.</exception>
        Task<bool> ConnectAsync(IConnectionInitializingTerminal terminal);

        /// <summary>
        /// Reads a string from the server.
        /// </summary>
        /// <returns>The read string.</returns>
        /// <exception cref="ObjectDisposedException">The connection object is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The connection object is currently not connected.</exception>
        Task<string> ReadAsync();

        /// <summary>
        /// Writes a string to the server.
        /// </summary>
        /// <param name="str">The string to write.</param>
        /// <exception cref="ObjectDisposedException">The connection object is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The connection object is currently not connected.</exception>
        void Write(string str);

        /// <summary>
        /// Indicates to the server that the terminal size has changed to the specified dimensions.
        /// </summary>
        /// <param name="rows">The new amount of rows.</param>
        /// <param name="columns">The new amount of columns.</param>
        /// <exception cref="InvalidOperationException">The connection object is currently not connected.</exception>
        void ResizeTerminal(int rows, int columns);

        /// <summary>
        /// Closes the connection to the server.
        /// </summary>
        void Disconnect();
    }
}
