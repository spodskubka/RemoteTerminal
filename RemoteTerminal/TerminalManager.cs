using System;
using System.Collections.Generic;
using System.Linq;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;

namespace RemoteTerminal
{
    public static class TerminalManager
    {
        private static IDictionary<Guid, ITerminal> terminals = new Dictionary<Guid, ITerminal>();

        public static Guid Create(ConnectionData connectionData)
        {
            Guid guid = Guid.NewGuid();
            ITerminal terminal;

            switch (connectionData.Type)
            {
                case ConnectionType.Telnet:
                    terminal = new SshTerminal(connectionData, localEcho: false);
                    break;
                case ConnectionType.Ssh:
                    terminal = new SshTerminal(connectionData, localEcho: false);
                    break;
                default:
                    throw new Exception("Unknown connection type.");
            }

            terminal.PowerOn();
            terminals.Add(guid, terminal);
            return guid;
        }

        public static ITerminal GetTerminal(Guid guid)
        {
            return terminals.Where(t => t.Key == guid).Select(t => t.Value).SingleOrDefault();
        }

        public static Guid? GetGuid(ITerminal terminal)
        {
            return terminals.Where(t => t.Value == terminal).Select(t => t.Key).SingleOrDefault();
        }

        public static bool Remove(ITerminal terminal)
        {
            if (!terminals.Any(t => t.Value == terminal))
            {
                return false;
            }

            terminals.Remove(terminals.Where(t => t.Value == terminal).Single());
            terminal.PowerOff();
            terminal.Dispose();
            return true;
        }

        public static ICollection<ITerminal> Terminals
        {
            get
            {
                return terminals.Values;
            }
        }
    }
}
