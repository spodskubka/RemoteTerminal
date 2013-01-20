using System;
using System.Collections.Generic;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH_AGENT_RSA_RESPONSE message.
    /// </summary>
    [Message("SSH_AGENT_RSA_RESPONSE", 4)]
    public class RsaResponseMessage : PrivateKeyAgentMessage
    {
        /// <summary>
        /// Gets or sets the response data.
        /// </summary>
        public byte[] Response { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMessagePublicKey"/> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="username">Authentication username.</param>
        /// <param name="keyAlgorithmName">Name of private key algorithm.</param>
        /// <param name="keyData">Private key data.</param>
        public RsaResponseMessage(byte[] response)
        {
            if (response.Length != 16)
            {
                throw new ArgumentException("Response must be exactly 16 bytes long.", "reponse");
            }
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
            base.SaveData();
            this.Write(this.Response);
        }
    }
}
