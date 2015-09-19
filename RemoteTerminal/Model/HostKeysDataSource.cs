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
