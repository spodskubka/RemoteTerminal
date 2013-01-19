using System;
using System.Collections.Generic;
using Renci.SshNet.Common;
using Renci.SshNet.Security;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH2_AGENTC_ADD_IDENTITY message.
    /// </summary>
    [Message("SSH2_AGENTC_ADD_IDENTITY", 17)]
    public class AddIdentityMessage : PrivateKeyAgentMessage
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
        public AddIdentityMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveIdentityMessage"/> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="username">Authentication username.</param>
        /// <param name="keyAlgorithmName">Name of private key algorithm.</param>
        /// <param name="keyData">Private key data.</param>
        public AddIdentityMessage(KeyHostAlgorithm key, string comment)
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
            var keyType = this.ReadString();
            switch (keyType)
            {
                case "ssh-rsa":
                    var n = this.ReadBigInt();
                    var e = this.ReadBigInt();
                    var d = this.ReadBigInt();
                    var iqmp = this.ReadBigInt();
                    var p = this.ReadBigInt();
                    var q = this.ReadBigInt();
                    this.Key = new KeyHostAlgorithm(keyType, new RsaKey(n, e, d, p, q, iqmp));
                    break;
                default:
                    throw new SshException("Private key type '" + keyType + "' is not supported.");
            }

            this.Comment = this.ReadString();
        }

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected override void SaveData()
        {
            throw new NotImplementedException();
        }
    }
}
