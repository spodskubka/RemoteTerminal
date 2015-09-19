namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH2_AGENTC_REQUEST_IDENTITIES message.
    /// </summary>
    [Message("SSH2_AGENTC_REQUEST_IDENTITIES", 11)]
    public class RequestIdentitiesMessage : PrivateKeyAgentMessage
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
