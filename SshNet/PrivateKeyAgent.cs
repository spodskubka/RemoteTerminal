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
        private List<KeyHostAlgorithm> keys = new List<KeyHostAlgorithm>();

        public void Add(KeyHostAlgorithm privateKeyFile)
        {
            this.keys.Add(privateKeyFile);
        }

        public IReadOnlyCollection<KeyHostAlgorithm> List()
        {
            return this.keys;
        }

        public byte[] Sign(byte[] keyData, byte[] signatureData)
        {
            KeyHostAlgorithm signKey = (from key in keys
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
