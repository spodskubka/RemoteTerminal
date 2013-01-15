using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet.Security;

namespace Renci.SshNet
{
    public class PrivateKeyAgent
    {
        /// <summary>
        /// Gets the key files used for authentication.
        /// </summary>
        private List<HostAlgorithm> keys = new List<HostAlgorithm>();

        public void Add(HostAlgorithm privateKeyFile)
        {
            this.keys.Add(privateKeyFile);
        }

        public IReadOnlyCollection<HostAlgorithm> List()
        {
            return this.keys;
        }

        public byte[] Sign(byte[] keyData, byte[] signatureData)
        {
            HostAlgorithm signKey = (from key in keys
                                     where key.Data.SequenceEqual(keyData)
                                     select key).FirstOrDefault();

            if (signKey == null)
            {
                return null;
            }

            return signKey.Sign(signatureData);
        }
    }
}
