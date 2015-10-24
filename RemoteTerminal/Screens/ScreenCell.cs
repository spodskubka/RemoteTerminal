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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RemoteTerminal.Screens
{
    /// <summary>
    /// The virtual in-memory representation of a screen cell.
    /// </summary>
    /// <remarks>
    /// This class contains a static "cell recycler". Its purpose is to reduce the performance
    /// impact associated with the continuous creation and GC collection of ScreenCell instances.
    /// </remarks>
    public class ScreenCell : IRenderableScreenCell
    {
        /// <summary>
        /// The maximum size of the cell recycler.
        /// </summary>
        private const int RecyclerSize = 10000;

        /// <summary>
        /// The lock for accessing the cell recycler.
        /// </summary>
        private static readonly object RecyclableCellsLock = new object();

        /// <summary>
        /// The content of the cell recycler (a list of cells that can be reused later).
        /// </summary>
        private static readonly List<ScreenCell> RecyclableCells = new List<ScreenCell>(RecyclerSize);

        /// <summary>
        /// Gets a specified number of cells that are preferably from the recycler.
        /// </summary>
        /// <param name="count">The number of cells to return.</param>
        /// <returns>The specified number of cells.</returns>
        /// <remarks>
        /// As many cells as possible are taken from the recycler and reset to a clean state. If the number of cells
        /// in the recycler is not enough the missing cells are newly created.
        /// </remarks>
        public static IEnumerable<ScreenCell> GetFreshCells(int count)
        {
            List<ScreenCell> cellsRecycled;
            lock (RecyclableCellsLock)
            {
                cellsRecycled = RecyclableCells.GetRange(0, Math.Min(count, RecyclableCells.Count));
                RecyclableCells.RemoveRange(0, cellsRecycled.Count);
            }

            foreach (var cell in cellsRecycled)
            {
                cell.Reset();
            }

            IEnumerable<ScreenCell> cellsNonRecycled = new int[count - cellsRecycled.Count].Select(c => new ScreenCell());

            return cellsRecycled.Concat(cellsNonRecycled);
        }

        /// <summary>
        /// Inserts the specified cells into the recycler.
        /// </summary>
        /// <param name="cells">The cells to insert into the recycler.</param>
        public static void RecycleCells(IEnumerable<ScreenCell> cells)
        {
            lock (RecyclableCellsLock)
            {
                if (RecyclableCells.Count >= RecyclerSize)
                {
                    return;
                }

                RecyclableCells.AddRange(cells);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenCell"/> class.
        /// </summary>
        // TODO: remove this attribute (at least from the release compile)? (I think I added it to get better performance statistics)
        [MethodImpl(MethodImplOptions.NoInlining)]
        private ScreenCell()
        {
            this.Reset();
        }

        /// <summary>
        /// Resets the state of this object to a newly created one.
        /// </summary>
        // TODO: remove this attribute (at least from the release compile)? (I think I added it to get better performance statistics)
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Reset()
        {
            this.Character = ' ';
            this.Modifications = ScreenCellModifications.None;
            this.ForegroundColor = ScreenColor.DefaultForeground;
            this.BackgroundColor = ScreenColor.DefaultBackground;
        }

        /// <summary>
        /// Converts the value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the value of this instance.</returns>
        public override string ToString()
        {
            return this.Character.ToString();
        }

        /// <summary>
        /// Applies the specified format to this screen cell.
        /// </summary>
        /// <param name="format">The format to apply.</param>
        public void ApplyFormat(ScreenCellFormat format)
        {
            if (format == null)
            {
                return;
            }

            this.Modifications = ScreenCellModifications.None;
            if (format.BoldMode)
            {
                this.Modifications |= ScreenCellModifications.Bold;
            }

            if (format.UnderlineMode)
            {
                this.Modifications |= ScreenCellModifications.Underline;
            }

            if (format.ReverseMode)
            {
                this.BackgroundColor = format.ForegroundColor;
                this.ForegroundColor = format.BackgroundColor;
            }
            else
            {
                this.BackgroundColor = format.BackgroundColor;
                this.ForegroundColor = format.ForegroundColor;
            }
        }

        /// <summary>
        /// Clones the screen cell.
        /// </summary>
        /// <returns>The cloned screen cell.</returns>
        public ScreenCell Clone()
        {
            // TODO: use the cell recycler here?
            return new ScreenCell()
            {
                Character = this.Character,
                Modifications = this.Modifications,
                ForegroundColor = this.ForegroundColor,
                BackgroundColor = this.BackgroundColor,
            };
        }

        public char Character { get; set; }
        public ScreenCellModifications Modifications { get; set; }
        public ScreenColor ForegroundColor { get; set; }
        public ScreenColor BackgroundColor { get; set; }
    }
}
