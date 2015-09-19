using System;
using System.Collections.Generic;
using Renci.SshNet.Security;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH_AGENT_RSA_IDENTITIES_ANSWER message.
    /// </summary>
    [Message("SSH_AGENT_RSA_IDENTITIES_ANSWER", 2)]
    public class RsaIdentitiesAnswerMessage : PrivateKeyAgentMessage
    {
        public List<PrivateKeyAgentKey> Keys { get; private set; }

        public RsaIdentitiesAnswerMessage(IReadOnlyCollection<PrivateKeyAgentKey> keys)
        {
            this.Keys = new List<PrivateKeyAgentKey>(keys);
        }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected override void SaveData()
        {
            base.Write((UInt32)this.Keys.Count);
            foreach (var key in this.Keys)
            {
                base.Write((uint)key.Key.Key.KeyLength);
                base.WriteBigInt1(((RsaKey)key.Key.Key).Exponent);
                base.WriteBigInt1(((RsaKey)key.Key.Key).Modulus);
                base.Write(key.Comment);
            }
        }
    }
}
