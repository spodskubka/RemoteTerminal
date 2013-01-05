using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private static ObservableCollection<ITerminal> terminals = new ObservableCollection<ITerminal>();

        public static ITerminal Create(ConnectionData connectionData)
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
            terminals.Add(terminal);
            return terminal;
        }

        public static bool Remove(ITerminal terminal)
        {
            if (!terminals.Contains(terminal))
            {
                return false;
            }

            terminals.Remove(terminal);
            terminal.PowerOff();
            terminal.Dispose();
            return true;
        }

        public static ObservableCollection<ITerminal> Terminals
        {
            get
            {
                return terminals;
            }
        }
    }
}
