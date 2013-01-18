using System;
using System.Collections.Generic;
using Renci.SshNet.Common;
using Renci.SshNet.Security;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH_AGENT_IDENTITIES_ANSWER message.
    /// </summary>
    [Message("SSH_AGENT_IDENTITIES_ANSWER", 12)]
    public class IdentitiesAnswerMessage : PrivateKeyAgentMessage
    {
        public List<KeyHostAlgorithm> Keys { get; private set; }

        public IdentitiesAnswerMessage(IReadOnlyCollection<KeyHostAlgorithm> keys)
        {
            this.Keys = new List<KeyHostAlgorithm>(keys);    
        }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
            int count = (int)this.ReadUInt32();
            this.Keys = new List<KeyHostAlgorithm>(count);
            for (int i = 0; i < count; i++)
            {
                this.Keys.Add(new KeyHostAlgorithm("", new RsaKey(), this.ReadBytes()));
                this.ReadString();
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
                base.WriteBinaryString(key.Data);
                base.Write("imported-openssh-key");
            }
        }
    }
}
