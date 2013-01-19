using System.Collections.Generic;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH2_AGENTC_REMOVE_IDENTITY message.
    /// </summary>
    [Message("SSH2_AGENTC_REMOVE_IDENTITY", 18)]
    public class RemoveIdentityMessage : PrivateKeyAgentMessage
    {
        /// <summary>
        /// Gets the public key data.
        /// </summary>
        public byte[] PublicKeyData { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveIdentityMessage"/> class.
        /// </summary>
        public RemoveIdentityMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveIdentityMessage"/> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="username">Authentication username.</param>
        /// <param name="keyAlgorithmName">Name of private key algorithm.</param>
        /// <param name="keyData">Private key data.</param>
        public RemoveIdentityMessage(byte[] publicKeyData)
        {
            this.PublicKeyData = publicKeyData;
        }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
            base.LoadData();
            this.PublicKeyData = this.ReadBinaryString();
        }

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected override void SaveData()
        {
            base.SaveData();
            this.WriteBinaryString(this.PublicKeyData);
        }
    }
}
