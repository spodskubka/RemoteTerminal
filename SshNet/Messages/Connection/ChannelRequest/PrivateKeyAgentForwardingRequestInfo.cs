namespace Renci.SshNet.Messages.Connection
{
    /// <summary>
    /// Represents "auth-agent-req@openssh.com" type channel request information
    /// </summary>
    internal class PrivateKeyAgentForwardingRequestInfo : RequestInfo
    {
        /// <summary>
        /// Channel request name
        /// </summary>
        public const string NAME = "auth-agent-req@openssh.com";

        /// <summary>
        /// Gets the name of the request.
        /// </summary>
        /// <value>
        /// The name of the request.
        /// </value>
        public override string RequestName
        {
            get { return PrivateKeyAgentForwardingRequestInfo.NAME; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateKeyAgentForwardingRequestInfo"/> class.
        /// </summary>
        public PrivateKeyAgentForwardingRequestInfo()
        {
            this.WantReply = true;
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
