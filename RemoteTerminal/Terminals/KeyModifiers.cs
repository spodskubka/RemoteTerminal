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
