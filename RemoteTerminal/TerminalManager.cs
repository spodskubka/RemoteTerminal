using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteTerminal.Connections;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;

namespace RemoteTerminal
{
    public static class TerminalManager
    {
        private static Dictionary<Guid, ITerminal> activeTerminals = new Dictionary<Guid, ITerminal>();

        public static Guid Create(ConnectionData connectionData)
        {
            ITerminal terminal;

            switch (connectionData.Type)
            {
                case ConnectionType.Telnet:
                    terminal = new TelnetTerminal(connectionData);
                    break;
                case ConnectionType.Ssh:
                    terminal = new SshTerminal(connectionData);
                    break;
                default:
                    throw new Exception("Unknown connection type.");
            }

            terminal.PowerOn();
            Guid guid = Guid.NewGuid();
            activeTerminals[guid] = terminal;
            return guid;
        }

        public static ITerminal GetActive(Guid guid)
        {
            if (!activeTerminals.ContainsKey(guid))
            {
                return null;
            }

            ITerminal terminal = activeTerminals[guid];
            return terminal;
        }

        public static bool Remove(Guid guid)
        {
            if (!activeTerminals.ContainsKey(guid))
            {
                return false;
            }

            ITerminal terminal = activeTerminals[guid];
            activeTerminals.Remove(guid);
            terminal.PowerOff();
            terminal.Dispose();
            return true;
        }
    }
}
