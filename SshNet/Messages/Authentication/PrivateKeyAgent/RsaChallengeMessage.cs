using System;
using System.Collections.Generic;
using Renci.SshNet.Common;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH2_AGENTC_SIGN_REQUEST message.
    /// </summary>
    [Message("SSH_AGENTC_RSA_CHALLENGE", 3)]
    public class RsaChallengeMessage : PrivateKeyAgentMessage
    {
        /// <summary>
        /// Gets the key's exponent.
        /// </summary>
        public BigInteger E { get; private set; }

        /// <summary>
        /// Gets the key's modulus.
        /// </summary>
        public BigInteger N { get; private set; }

        /// <summary>
        /// Gets the encrypted challenge.
        /// </summary>
        public BigInteger EncryptedChallenge{get;private set;}

        /// <summary>
        /// Gets the session id.
        /// </summary>
        public byte[] SessionId{get;private set;}

        /// <summary>
        /// Gets the response type (must be 1).
        /// </summary>
        public uint ResponseType{get;private set;}

        /// <summary>
        /// Initializes a new instance of the <see cref="SignRequestMessage"/> class.
        /// </summary>
        public RsaChallengeMessage()
        {
        }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
            base.LoadData();
            var ignored = this.ReadUInt32();
            this.E = this.ReadBigInt1();
            this.N = this.ReadBigInt1();
            this.EncryptedChallenge = this.ReadBigInt1();
            this.SessionId = this.ReadBytes(16);
            this.ResponseType = this.ReadUInt32();
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
