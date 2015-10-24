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

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// Contains font metrics (logical or physical).
    /// </summary>
    /// <remarks>
    /// Font metrics contain the information, how big a terminal cell must be for a specific font size.
    /// This size varies for each font family (a Consolas character has a different size than a Courier New character).
    /// But it should scale well with different font sizes of the same font family (a 20 percent increase in Consolas
    /// font size results in a 20 percent increase in required terminal cell size).
    /// </remarks>
    public class ScreenFontMetrics
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenFontMetrics"/> class.
        /// </summary>
        /// <param name="fontSize">The font size.</param>
        /// <param name="cellWidth">The cell width.</param>
        /// <param name="cellHeight">The cell height.</param>
        public ScreenFontMetrics(float fontSize, float cellWidth, float cellHeight)
        {
            this.FontSize = fontSize;
            this.CellWidth = cellWidth;
            this.CellHeight = cellHeight;
        }

        /// <summary>
        /// Gets the font size.
        /// </summary>
        public float FontSize { get; private set; }

        /// <summary>
        /// Gets the cell width.
        /// </summary>
        public float CellWidth { get; private set; }

        /// <summary>
        /// Gets the cell height.
        /// </summary>
        public float CellHeight { get; private set; }

        /// <summary>
        /// Scales font metrics by the specified value.
        /// </summary>
        /// <param name="fontMetrics">The font metrics to scale.</param>
        /// <param name="d">The factor by which to scale the font metrics (e.g. 1.1f for a 10 percent increase).</param>
        /// <returns>The scaled font metrics.</returns>
        public static ScreenFontMetrics operator *(ScreenFontMetrics fontMetrics, float d)
        {
            return new ScreenFontMetrics(fontMetrics.FontSize * d, fontMetrics.CellWidth * d, fontMetrics.CellHeight * d);
        }
    }
}
