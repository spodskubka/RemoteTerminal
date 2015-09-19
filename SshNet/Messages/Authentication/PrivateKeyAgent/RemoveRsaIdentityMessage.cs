using Renci.SshNet.Common;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH_AGENTC_REMOVE_RSA_IDENTITY message.
    /// </summary>
    [Message("SSH_AGENTC_REMOVE_RSA_IDENTITY", 8)]
    public class RemoveRsaIdentityMessage : PrivateKeyAgentMessage
    {
        /// <summary>
        /// Gets the length of the key.
        /// </summary>
        public uint KeyBits { get; private set; }

        /// <summary>
        /// Gets the key's exponent.
        /// </summary>
        public BigInteger E { get; private set; }

        /// <summary>
        /// Gets the key's modulus.
        /// </summary>
        public BigInteger N { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveIdentityMessage"/> class.
        /// </summary>
        public RemoveRsaIdentityMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveIdentityMessage"/> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="username">Authentication username.</param>
        /// <param name="keyAlgorithmName">Name of private key algorithm.</param>
        /// <param name="keyData">Private key data.</param>
        public RemoveRsaIdentityMessage(uint keyBits, BigInteger e, BigInteger n)
        {
            this.KeyBits = keyBits;
            this.E = e;
            this.N = n;
        }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
            base.LoadData();
            this.KeyBits = this.ReadUInt32();
            this.E = this.ReadBigInt1();
            this.N = this.ReadBigInt1();

            if (this.KeyBits != this.N.BitLength)
            {
                throw new SshException("Key length does not match.");
            }
        }

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected override void SaveData()
        {
            base.SaveData();
            this.Write(this.KeyBits);
            this.WriteBigInt1(this.E);
            this.WriteBigInt1(this.N);
        }
    }
}
