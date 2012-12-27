using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTerminal.Model
{
    public enum ConnectionType
    {
        /// <summary>
        /// SSH connection.
        /// </summary>
        Ssh,

        /// <summary>
        /// Telnet connection.
        /// </summary>
        Telnet,
    }
}
