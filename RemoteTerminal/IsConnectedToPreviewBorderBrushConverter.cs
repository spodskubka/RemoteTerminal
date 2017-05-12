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
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace RemoteTerminal
{
    /// <summary>
    /// Converts a boolean "is connected" value to a preview border brush.
    /// </summary>
    public class IsConnectedToPreviewBorderBrushConverter : IValueConverter
    {
        /// <summary>
        /// A solid black brush.
        /// </summary>
        private static readonly SolidColorBrush Gray = new SolidColorBrush(Colors.Gray);

        /// <summary>
        /// A solid red brush.
        /// </summary>
        private static readonly SolidColorBrush Red = new SolidColorBrush(Colors.Red);

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isConnected = (bool)value;

            return isConnected ? Gray : Red;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
