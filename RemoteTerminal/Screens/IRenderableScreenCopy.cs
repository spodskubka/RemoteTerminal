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
    /// Interface for the copy of a renderable screen.
    /// </summary>
    public interface IRenderableScreenCopy
    {
        /// <summary>
        /// Gets the cells that the terminal screen is composed of.
        /// </summary>
        IRenderableScreenCell[][] Cells { get; }

        /// <summary>
        /// Gets the row position of the cursor (zero-based).
        /// </summary>
        int CursorRow { get; }

        /// <summary>
        /// Gets the column position of the cursor (zero-based).
        /// </summary>
        int CursorColumn { get; }

        /// <summary>
        /// Gets a value indicating whether the cursor should be hidden.
        /// </summary>
        bool CursorHidden { get; }

        /// <summary>
        /// Gets a value indicating whether the screen has focus.
        /// </summary>
        bool HasFocus { get; }
    }
}
