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
        private List<PrivateKeyAgentKey> keys = new List<PrivateKeyAgentKey>();

        public bool Add(KeyHostAlgorithm key, string comment)
        {
            var existingKey = GetKey(key.Data);
            if (existingKey != null)
            {
                return false;
            }

            this.keys.Add(new PrivateKeyAgentKey(key, comment));
            return true;
        }

        public IReadOnlyCollection<PrivateKeyAgentKey> List()
        {
            return this.keys;
        }

        public bool Remove(byte[] keyData)
        {
            var key = GetKey(keyData);

            return this.keys.Remove(key);
        }

        public void RemoveAll()
        {
            this.keys.Clear();
        }

        public byte[] Sign(byte[] keyData, byte[] signatureData)
        {
            var signKey = GetKey(keyData);

            if (signKey == null)
            {
                return null;
            }

            return signKey.Key.Sign(signatureData);
        }

        private PrivateKeyAgentKey GetKey(byte[] keyData)
        {
            return (from key in keys
                    where key.Key.Data.SequenceEqual(keyData)
                    select key).FirstOrDefault();
        }
    }
}
