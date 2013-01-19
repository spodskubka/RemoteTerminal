using System;
using System.Collections.Generic;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH_AGENTC_REMOVE_ALL_RSA_IDENTITIES message.
    /// </summary>
    [Message("SSH_AGENTC_REMOVE_ALL_RSA_IDENTITIES", 9)]
    public class RemoveAllRsaIdentitiesMessage : PrivateKeyAgentMessage
    {
        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
        }

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected override void SaveData()
        {
        }
    }
}
