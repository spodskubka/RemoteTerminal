using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTerminal.Model
{
    public enum AuthenticationType
    {
        /// <summary>
        /// Password authentication.
        /// </summary>
        Password,

        /// <summary>
        /// Keyboard-Interactive authentication.
        /// </summary>
        KeyboardInteractive,

        /// <summary>
        /// Private Key authentication.
        /// </summary>
        PrivateKey,

        /// <summary>
        /// Private Key Agent authentication.
        /// </summary>
        PrivateKeyAgent,

        ///// <summary>
        ///// Host-based authentication.
        ///// </summary>
        //HostBased,
    }
}
