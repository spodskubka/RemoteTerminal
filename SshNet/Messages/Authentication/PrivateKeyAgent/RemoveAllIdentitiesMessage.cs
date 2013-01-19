using System;
using System.Collections.Generic;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH2_AGENTC_REMOVE_ALL_IDENTITIES message.
    /// </summary>
    [Message("SSH2_AGENTC_REMOVE_ALL_IDENTITIES", 19)]
    public class RemoveAllIdentitiesMessage : PrivateKeyAgentMessage
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
