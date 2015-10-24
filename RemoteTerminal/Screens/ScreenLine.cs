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

using System.Collections.Generic;

namespace RemoteTerminal.Screens
{
    /// <summary>
    /// The virtual in-memory representation of a screen line.
    /// </summary>
    public class ScreenLine : List<ScreenCell>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenLine"/> class with the specified number of columns.
        /// </summary>
        /// <param name="columns">The number of columns.</param>
        /// <remarks>
        /// Gets cells from the cell recycler in the <see cref="ScreenCell"/> class.
        /// </remarks>
        public ScreenLine(int columns)
            : base(columns)
        {
            this.AddRange(ScreenCell.GetFreshCells(columns));
        }
    }
}
