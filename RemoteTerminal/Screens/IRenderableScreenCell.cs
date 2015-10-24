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
