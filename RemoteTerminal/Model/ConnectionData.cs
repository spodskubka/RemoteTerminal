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

namespace RemoteTerminal.Model
{
    /// <summary>
    /// Data model class for storing connection data.
    /// </summary>
    public class ConnectionData
    {
        /// <summary>
        /// Gets or sets the id (GUID).
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the (display) name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the connection type.
        /// </summary>
        public ConnectionType Type { get; set; }

        /// <summary>
        /// Gets or sets the host (IP or name).
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the TCP port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the authentication type.
        /// </summary>
        public AuthenticationType Authentication { get; set; }

        /// <summary>
        /// Gets or sets the name of the private key to use for authentication (only with AuthenticationType PrivateKey).
        /// </summary>
        public string PrivateKeyName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether private key agent forwarding should be active.
        /// </summary>
        public bool PrivateKeyAgentForwarding { get; set; }

        /// <summary>
        /// Gets or sets the "Segoe UI Symbol" character for the connection type (displayed on the FavoritesPage).
        /// </summary>
        public char ImageChar
        {
            get
            {
                return (char)(this.Type == ConnectionType.Ssh ? 0xE131 : 0xE130);
            }
        }

        /// <summary>
        /// Gets or sets a string with host and port (displayed on the FavoritesPage).
        /// </summary>
        public string HostAndPort
        {
            get
            {
                return this.Host + ":" + this.Port.ToString();
            }
        }
    }
}
