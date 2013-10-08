using System.Globalization;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace RemoteTerminal.Model
{
    internal class HostKeysDataSource
    {
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

        private static IPropertySet GetHostKeysSettings()
        {
            var hostKeysContainer = ApplicationData.Current.LocalSettings.CreateContainer("HostKeys", ApplicationDataCreateDisposition.Always);
            return hostKeysContainer.Values;
        }

        public static void AddOrUpdate(string host, int port, string hostKey)
        {
            string identifier = host + ":" + port.ToString(CultureInfo.InvariantCulture);
            var hostKeys = GetHostKeysSettings();
            hostKeys[identifier] = hostKey;
        }
    }
}
