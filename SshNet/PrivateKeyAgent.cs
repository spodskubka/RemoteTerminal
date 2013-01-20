using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet.Common;
using Renci.SshNet.Security;
using Renci.SshNet.Security.Cryptography.Ciphers;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;

namespace Renci.SshNet
{
    public class PrivateKeyAgent
    {
        /// <summary>
        /// Gets the SSH1 key files used for authentication.
        /// </summary>
        private List<PrivateKeyAgentKey> keysSsh1 = new List<PrivateKeyAgentKey>();

        /// <summary>
        /// Gets the SSH2 key files used for authentication.
        /// </summary>
        private List<PrivateKeyAgentKey> keysSsh2 = new List<PrivateKeyAgentKey>();

        public bool AddSsh1(KeyHostAlgorithm key, string comment)
        {
            if (!(key.Key is RsaKey))
            {
                throw new SshException("SSH1 keys can only be RSA keys.");
            }

            return Add(this.keysSsh1, key, comment);
        }

        public bool AddSsh2(KeyHostAlgorithm key, string comment)
        {
            return Add(this.keysSsh2, key, comment);
        }

        public IReadOnlyCollection<PrivateKeyAgentKey> ListSsh1()
        {
            return this.keysSsh1;
        }

        public IReadOnlyCollection<PrivateKeyAgentKey> ListSsh2()
        {
            return this.keysSsh2;
        }

        public bool RemoveSsh1(BigInteger e, BigInteger n)
        {
            var key = GetKey(e, n);
            return this.keysSsh1.Remove(key);
        }

        public bool RemoveSsh2(byte[] keyData)
        {
            var key = GetKey(this.keysSsh2, keyData);
            return this.keysSsh2.Remove(key);
        }

        public void RemoveAllSsh1()
        {
            this.keysSsh2.Clear();
        }

        public void RemoveAllSsh2()
        {
            this.keysSsh2.Clear();
        }

        public byte[] DecryptSsh1(BigInteger e, BigInteger n, BigInteger encryptedChallenge, byte[] sessionId)
        {
            var decryptKey = GetKey(e, n);

            if (decryptKey == null)
            {
                return null;
            }

            RsaCipher cipher = new RsaCipher((RsaKey)decryptKey.Key.Key);
            byte[] decryptedChallenge = cipher.Decrypt(encryptedChallenge.ToByteArray().Reverse().ToArray());

            var md5 = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            byte[] response;
            CryptographicBuffer.CopyToByteArray(md5.HashData(CryptographicBuffer.CreateFromByteArray(decryptedChallenge.Concat(sessionId).ToArray())), out response);
            return response;
        }

        public byte[] SignSsh2(byte[] keyData, byte[] signatureData)
        {
            var signKey = GetKey(this.keysSsh2, keyData);

            if (signKey == null)
            {
                return null;
            }

            return signKey.Key.Sign(signatureData);
        }

        private static bool Add(List<PrivateKeyAgentKey> keys, KeyHostAlgorithm key, string comment)
        {
            var existingKey = GetKey(keys, key.Data);
            if (existingKey != null)
            {
                return false;
            }

            keys.Add(new PrivateKeyAgentKey(key, comment));
            return true;
        }

        private static PrivateKeyAgentKey GetKey(List<PrivateKeyAgentKey> keys, byte[] keyData)
        {
            return (from key in keys
                    where key.Key.Data.SequenceEqual(keyData)
                    select key).FirstOrDefault();
        }

        private PrivateKeyAgentKey GetKey(BigInteger e, BigInteger n)
        {
            return (from key in this.keysSsh1
                    where ((RsaKey)key.Key.Key).Exponent == e
                    where ((RsaKey)key.Key.Key).Modulus == n
                    select key).FirstOrDefault();
        }
    }
}
