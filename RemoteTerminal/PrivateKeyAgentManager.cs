using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
