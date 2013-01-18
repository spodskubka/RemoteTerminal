using System;
using System.Collections.Generic;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH_MSG_USERAUTH_FAILURE message.
    /// </summary>
    [Message("SSH_AGENT_SUCCESS", 6)]
    public class SuccessMessage : PrivateKeyAgentMessage
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
