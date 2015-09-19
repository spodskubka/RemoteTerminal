using System;
using Renci.SshNet;

namespace RemoteTerminal
{
    /// <summary>
    /// Keeps the <see cref="PrivateKeyAgent"/> singleton.
    /// </summary>
    static class PrivateKeyAgentManager
    {
        /// <summary>
        /// The <see cref="PrivateKeyAgent"/> singleton.
        /// </summary>
        private static Lazy<PrivateKeyAgent> privateKeyAgent = new Lazy<PrivateKeyAgent>();

        /// <summary>
        /// Gets the <see cref="PrivateKeyAgent"/> singleton.
        /// </summary>
        public static PrivateKeyAgent PrivateKeyAgent
        {
            get
            {
                return privateKeyAgent.Value;
            }
        }
    }
}
