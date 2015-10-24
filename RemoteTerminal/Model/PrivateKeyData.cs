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

using System.Linq;

namespace RemoteTerminal.Model
{
    /// <summary>
    /// Data model class for storing private key data.
    /// </summary>
    public class PrivateKeyData
    {
        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the actual private key data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets the number of favorites that use this private key (displayed on the PrivateKeysPage).
        /// </summary>
        public int LinkedFavorites
        {
            get
            {
                var favoritesDataSource = App.Current.Resources["favoritesDataSource"] as FavoritesDataSource;
                return (from favorite in favoritesDataSource.Favorites
                        where favorite.Authentication == AuthenticationType.PrivateKey
                        where favorite.PrivateKeyName == this.FileName
                        select favorite).Count();
            }
        }
    }
}
