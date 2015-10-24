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
    /// The screen color (4 special colors and 16 default colors).
    /// </summary>
    public enum ScreenColor
    {
        /// <summary>
        /// The cursor background color.
        /// </summary>
        CursorBackground = -4,

        /// <summary>
        /// The cursor foreground color.
        /// </summary>
        CursorForeground = -3,

        /// <summary>
        /// The default background color.
        /// </summary>
        DefaultBackground = -2,

        /// <summary>
        /// The default foreground color.
        /// </summary>
        DefaultForeground = -1,

        /// <summary>
        /// Black (normal).
        /// </summary>
        Black = 0,

        /// <summary>
        /// Red (normal).
        /// </summary>
        Red,

        /// <summary>
        /// Green (normal).
        /// </summary>
        Green,

        /// <summary>
        /// Yellow (normal).
        /// </summary>
        Yellow,

        /// <summary>
        /// Blue (normal).
        /// </summary>
        Blue,

        /// <summary>
        /// Magenta (normal).
        /// </summary>
        Magenta,

        /// <summary>
        /// Cyan (normal).
        /// </summary>
        Cyan,

        /// <summary>
        /// White (normal)
        /// </summary>
        White,

        /// <summary>
        /// Black bright.
        /// </summary>
        BlackBright,

        /// <summary>
        /// Red bright.
        /// </summary>
        RedBright,

        /// <summary>
        /// Green bright.
        /// </summary>
        GreenBright,

        /// <summary>
        /// Yellow bright.
        /// </summary>
        YellowBright,

        /// <summary>
        /// Blue bright.
        /// </summary>
        BlueBright,

        /// <summary>
        /// Magenta bright.
        /// </summary>
        MagentaBright,

        /// <summary>
        /// Cyan bright.
        /// </summary>
        CyanBright,

        /// <summary>
        /// White bright.
        /// </summary>
        WhiteBright,
    }
}
