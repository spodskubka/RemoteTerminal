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

using System.Globalization;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace RemoteTerminal.Model
{
    /// <summary>
    /// The data source for the remembered host keys.
    /// </summary>
    internal class HostKeysDataSource
    {
        /// <summary>
        /// Gets the host key for the specified host:port combination.
        /// </summary>
        /// <param name="host">The host (name/IP).</param>
        /// <param name="port">The TCP port.</param>
        /// <returns>The host key.</returns>
        public static string GetHostKey(string host, int port)
        {
            string identifier = host + ":" + port.ToString(CultureInfo.InvariantCulture);
            var hostKeys = GetHostKeysSettings();
            if (hostKeys.ContainsKey(identifier))
            {
                return (string)hostKeys[identifier];
            }

            return null;
        }

        /// <summary>
        /// Reads the local "HostKeys" app settings container.
        /// </summary>
        /// <returns>The values from the local "HostKeys" app settings container.</returns>
        private static IPropertySet GetHostKeysSettings()
        {
            var hostKeysContainer = ApplicationData.Current.LocalSettings.CreateContainer("HostKeys", ApplicationDataCreateDisposition.Always);
            return hostKeysContainer.Values;
        }

        /// <summary>
        /// Adds or updates a host key.
        /// </summary>
        /// <param name="host">The host (name/IP).</param>
        /// <param name="port">The TCP port.</param>
        /// <param name="hostKey">The host key.</param>
        public static void AddOrUpdate(string host, int port, string hostKey)
        {
            string identifier = host + ":" + port.ToString(CultureInfo.InvariantCulture);
            var hostKeys = GetHostKeysSettings();
            hostKeys[identifier] = hostKey;
        }
    }
}
