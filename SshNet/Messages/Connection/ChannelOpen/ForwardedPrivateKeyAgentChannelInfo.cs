namespace Renci.SshNet.Messages.Connection
{
    /// <summary>
    /// Used to open "auth-agent-req@openssh.com" channel type
    /// </summary>
    internal class ForwardedPrivateKeyAgentChannelInfo : ChannelOpenInfo
    {
        /// <summary>
        /// Specifies channel open type
        /// </summary>
        public const string NAME = "auth-agent@openssh.com";

        /// <summary>
        /// Gets the type of the channel to open.
        /// </summary>
        /// <value>
        /// The type of the channel to open.
        /// </value>
        public override string ChannelType
        {
            get { return ForwardedPrivateKeyAgentChannelInfo.NAME; }
        }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
            base.LoadData();
        }

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected override void SaveData()
        {
            base.SaveData();
        }
    }
}
