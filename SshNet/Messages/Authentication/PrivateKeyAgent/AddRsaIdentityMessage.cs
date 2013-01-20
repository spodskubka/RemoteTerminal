using System;
using System.Collections.Generic;
using Renci.SshNet.Common;
using Renci.SshNet.Security;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH_AGENTC_ADD_RSA_IDENTITY message.
    /// </summary>
    [Message("SSH_AGENTC_ADD_RSA_IDENTITY", 7)]
    public class AddRsaIdentityMessage : PrivateKeyAgentMessage
    {
        /// <summary>
        /// Gets the key data.
        /// </summary>
        public KeyHostAlgorithm Key { get; private set; }

        /// <summary>
        /// Gets the key comment.
        /// </summary>
        public string Comment { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveIdentityMessage"/> class.
        /// </summary>
        public AddRsaIdentityMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveIdentityMessage"/> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="username">Authentication username.</param>
        /// <param name="keyAlgorithmName">Name of private key algorithm.</param>
        /// <param name="keyData">Private key data.</param>
        public AddRsaIdentityMessage(KeyHostAlgorithm key, string comment)
        {
            this.Key = key;
            this.Comment = comment;
        }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
            base.LoadData();
            var ignored = this.ReadUInt32();
            this.Key = new KeyHostAlgorithm("ssh1", this.ReadRsaKey());
            this.Comment = this.ReadString();
        }

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected override void SaveData()
        {
            throw new NotImplementedException();
        }

        private RsaKey ReadRsaKey()
        {
            var n = this.ReadBigInt1();
            var e = this.ReadBigInt1();
            var d = this.ReadBigInt1();
            var iqmp = this.ReadBigInt1();
            var q = this.ReadBigInt1();
            var p = this.ReadBigInt1();

            return new RsaKey(n, e, d, p, q, iqmp);
        }
    }
}
