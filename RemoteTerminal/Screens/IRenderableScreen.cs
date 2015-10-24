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
