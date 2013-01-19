using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet.Security;

namespace Renci.SshNet
{
    public class PrivateKeyAgentKey
    {
        public PrivateKeyAgentKey(KeyHostAlgorithm key, string comment)
        {
            this.Key = key;
            this.Comment = comment;
        }

        public KeyHostAlgorithm Key { get; private set; }
        public string Comment { get; private set; }
    }
}
