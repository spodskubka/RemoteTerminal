using System;
using System.Collections.Generic;
using System.Linq;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;

namespace RemoteTerminal
{
    /// <summary>
    /// A class to manage a list of all terminals opened in the app.
    /// </summary>
    /// <remarks>
    /// Terminals are identified through a GUID.
    /// </remarks>
    public static class TerminalManager
    {
        /// <summary>
        /// Contains all active terminals.
        /// </summary>
        private static IDictionary<Guid, ITerminal> terminals = new Dictionary<Guid, ITerminal>();

        /// <summary>
        /// Creates a new terminal and returns its GUID.
        /// </summary>
        /// <param name="connectionData">The <see cref="ConnectionData"/> object to pass to the terminal.</param>
        /// <returns>The GUID of the created terminal.</returns>
        /// <remarks>
        /// This method decides which terminal implementation (<see cref="SshTerminal"/> or <see cref="TelnetTerminal"/>) to use based on the passed <see cref="ConnectionData"/> object.
        /// At the moment it only creates <see cref="SshTerminal"/> instances.
        /// </remarks>
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

        /// <summary>
        /// Gets the terminal with the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID of the terminal to get.</param>
        /// <returns>The found terminal; null if no terminal with the specified GUID was found.</returns>
        public static ITerminal GetTerminal(Guid guid)
        {
            return terminals.Where(t => t.Key == guid).Select(t => t.Value).SingleOrDefault();
        }

        /// <summary>
        /// Gets the GUID of the specified terminal.
        /// </summary>
        /// <param name="terminal">The terminal whose GUID to get.</param>
        /// <returns>The GUID of the specified terminal; null if the terminal was not found (only possible for terminal instances not created through this class).</returns>
        public static Guid? GetGuid(ITerminal terminal)
        {
            return terminals.Where(t => t.Value == terminal).Select(t => t.Key).SingleOrDefault();
        }

        /// <summary>
        /// Removes a terminal from the list of managed terminals.
        /// </summary>
        /// <param name="terminal">The terminal to remove.</param>
        /// <returns>A value indicating whether the terminal was found and removed; false if it was not found.</returns>
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

        /// <summary>
        /// Gets the list of terminals.
        /// </summary>
        public static ICollection<ITerminal> Terminals
        {
            get
            {
                return terminals.Values;
            }
        }
    }
}
