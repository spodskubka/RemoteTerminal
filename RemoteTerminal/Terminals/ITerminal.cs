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
using System.ComponentModel;
using RemoteTerminal.Screens;
using Windows.System;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// Represents a terminal.
    /// </summary>
    public interface ITerminal : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the renderable screen associated with this terminal.
        /// </summary>
        IRenderableScreen RenderableScreen { get; }

        /// <summary>
        /// Turns on this terminal and initializes the connection.
        /// </summary>
        void PowerOn();

        /// <summary>
        /// Disconnects the connection and turns off the terminal.
        /// </summary>
        void PowerOff();

        /// <summary>
        /// Resizes the screen of the terminal to the specified screen size.
        /// </summary>
        /// <param name="rows">The amount of rows on the screen.</param>
        /// <param name="columns">The amount of columns on the screen.</param>
        void ResizeScreen(int rows, int columns);

        /// <summary>
        /// Processes a user's key press.
        /// </summary>
        /// <param name="ch">The input character.</param>
        void ProcessKeyPress(char ch);

        /// <summary>
        /// Processes a user's key press.
        /// </summary>
        /// <param name="key">The pressed key.</param>
        /// <param name="keyModifiers">The key modifiers.</param>
        /// <returns>A value indicating whether the key press was processed.</returns>
        bool ProcessKeyPress(VirtualKey key, KeyModifiers keyModifiers);

        /// <summary>
        /// Processes text that is pasted to the terminal.
        /// </summary>
        /// <param name="str">The pasted text.</param>
        void ProcessPastedText(string str);

        /// <summary>
        /// Sets a value indicating whether the screen of this terminal has focus or not (may result in a differently drawn cursor).
        /// </summary>
        bool ScreenHasFocus { set; }

        /// <summary>
        /// Gets a value indicating whether the terminal is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Occurs when the terminal's connection is disconnected.
        /// </summary>
        event EventHandler Disconnected;

        /// <summary>
        /// Gets the (display) name of the terminal (comes from the connection data).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the (display) title of the terminal as defined by the server.
        /// </summary>
        string Title { get; }
    }
}
