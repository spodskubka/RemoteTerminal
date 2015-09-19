using System;
using System.Collections.Generic;
using Renci.SshNet.Security;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH2_AGENT_IDENTITIES_ANSWER message.
    /// </summary>
    [Message("SSH2_AGENT_IDENTITIES_ANSWER", 12)]
    public class IdentitiesAnswerMessage : PrivateKeyAgentMessage
    {
        public List<PrivateKeyAgentKey> Keys { get; private set; }

        public IdentitiesAnswerMessage(IReadOnlyCollection<PrivateKeyAgentKey> keys)
        {
            this.Keys = new List<PrivateKeyAgentKey>(keys);
        }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
            int count = (int)this.ReadUInt32();
            this.Keys = new List<PrivateKeyAgentKey>(count);
            for (int i = 0; i < count; i++)
            {
                var key = new KeyHostAlgorithm("", new RsaKey(), this.ReadBytes());
                string comment = this.ReadString();
                this.Keys.Add(new PrivateKeyAgentKey(key, comment));
            }
        }

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected override void SaveData()
        {
            base.Write((UInt32)this.Keys.Count);
            foreach (var key in this.Keys)
            {
                base.WriteBinaryString(key.Key.Data);
                base.Write(key.Comment);
            }
        }
    }
}
