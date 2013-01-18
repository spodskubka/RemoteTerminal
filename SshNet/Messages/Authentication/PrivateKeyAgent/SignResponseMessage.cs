using System.Collections.Generic;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    /// <summary>
    /// Represents SSH2_AGENT_SIGN_RESPONSE message.
    /// </summary>
    [Message("SSH2_AGENT_SIGN_RESPONSE", 14)]
    public class SignResponseMessage : PrivateKeyAgentMessage
    {
        /// <summary>
        /// Gets the public key data.
        /// </summary>
        public byte[] PublicKeyData { get; private set; }

        /// <summary>
        /// Gets or sets the data that should be signed by the PrivateKeyAgent.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the flags.
        /// </summary>
        public uint Flags { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMessagePublicKey"/> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="username">Authentication username.</param>
        /// <param name="keyAlgorithmName">Name of private key algorithm.</param>
        /// <param name="keyData">Private key data.</param>
        public SignResponseMessage(byte[] publicKeyData, byte[] data, uint flags)
        {
            this.PublicKeyData = publicKeyData;
            this.Data = data;
            this.Flags = flags;
        }

        /// <summary>
        /// Called when type specific data need to be loaded.
        /// </summary>
        protected override void LoadData()
        {
            base.LoadData();
            this.PublicKeyData = this.ReadBinaryString();
            this.Data = this.ReadBinaryString();
            this.Flags = this.ReadUInt32();
        }

        /// <summary>
        /// Called when type specific data need to be saved.
        /// </summary>
        protected override void SaveData()
        {
            base.SaveData();
            this.WriteBinaryString(this.PublicKeyData);
            this.WriteBinaryString(this.Data);
            this.Write(this.Flags);
        }
    }
}
