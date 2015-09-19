using System;
using Renci.SshNet;

namespace RemoteTerminal
{
    static class PrivateKeyAgentManager
    {
        private static Lazy<PrivateKeyAgent> privateKeyAgent = new Lazy<PrivateKeyAgent>();
        public static PrivateKeyAgent PrivateKeyAgent
        {
            get
            {
                return privateKeyAgent.Value;
            }
        }
    }
}
