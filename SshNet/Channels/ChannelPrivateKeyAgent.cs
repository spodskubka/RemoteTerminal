using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Renci.SshNet.Common;
using Renci.SshNet.Messages.Authentication.PrivateKeyAgent;
using Renci.SshNet.Messages.Connection;

namespace Renci.SshNet.Channels
{
    /// <summary>
    /// Implements "auth-agent@openssh.com" SSH channel.
    /// </summary>
    internal partial class ChannelPrivateKeyAgent : Channel
    {
        /// <summary>
        /// Holds metada about session messages
        /// </summary>
        private readonly IEnumerable<PrivateKeyAgentMessageMetadata> _messagesMetadata;

        /// <summary>
        /// Gets the type of the channel.
        /// </summary>
        /// <value>
        /// The type of the channel.
        /// </value>
        public override ChannelTypes ChannelType
        {
            get { return ChannelTypes.PrivateKeyAgent; }
        }

        private byte[] dataBuffer = new byte[0];

        public PrivateKeyAgent PrivateKeyAgent { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelPrivateKeyAgent"/> class.
        /// </summary>
        public ChannelPrivateKeyAgent()
            : base()
        {
            this._messagesMetadata = GetPrivateKeyAgentMessagesMetadata();
        }

        public void Start()
        {
            var successMessage = new ChannelOpenConfirmationMessage(this.RemoteChannelNumber, this.LocalWindowSize, this.PacketSize, this.LocalChannelNumber);
            this.SendMessage(successMessage);
        }

        protected override void OnEof()
        {
            base.OnEof();
            // Closing the channel doesn't work...
            //this.Close();
        }

        //public override void Close()
        //{
        //    //  Send EOF message first when channel need to be closed
        //    this.SendMessage(new ChannelEofMessage(this.RemoteChannelNumber));

        //    base.Close();
        //}

        /// <summary>
        /// Called when channel data is received.
        /// </summary>
        /// <param name="data">The data.</param>
        protected override void OnData(byte[] data)
        {
            base.OnData(data);

            if (this.dataBuffer.Length == 0)
            {
                this.dataBuffer = data;
            }
            else
            {
                int oldLength = this.dataBuffer.Length;
                Array.Resize<byte>(ref this.dataBuffer, this.dataBuffer.Length + data.Length);
                Array.Copy(data, 0, this.dataBuffer, oldLength, data.Length);
            }

            do
            {
                try
                {
                    PrivateKeyAgentMessage message = this.LoadPrivateKeyAgentMessage();
                    if (message == null)
                    {
                        break;
                    }

                    this.HandlePrivateKeyAgentMessage((dynamic)message);
                }
                catch (Exception ex)
                {
                    this.SendPrivateKeyAgentMessage(new FailureMessage());
                }
            }
            while (true);
        }

        protected void SendPrivateKeyAgentMessage(PrivateKeyAgentMessage message)
        {
            this.SendData(message.GetBytes());
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        /// <summary>
        /// Loads the message.
        /// </summary>
        /// <param name="data">Message data.</param>
        /// <returns>New message</returns>
        private PrivateKeyAgentMessage LoadPrivateKeyAgentMessage()
        {
            if (this.dataBuffer.Length < 4)
            {
                return null;
            }

            var messageLength = (uint)(this.dataBuffer[0] << 24 | this.dataBuffer[1] << 16 | this.dataBuffer[2] << 8 | this.dataBuffer[3]);
            if (this.dataBuffer.Length < messageLength + 4)
            {
                return null;
            }

            byte[] data;
            if (messageLength + 4 == this.dataBuffer.Length)
            {
                data = this.dataBuffer;
                this.dataBuffer = new byte[0];
            }
            else
            {
                data = new byte[messageLength + 4];
                Array.Copy(this.dataBuffer, data, data.Length);
                this.dataBuffer = this.dataBuffer.Skip(data.Length).ToArray();
            }

            if (messageLength == 0)
            {
                throw new SshException("Message is empty.");
            }

            var messageType = data[4];
            var messageMetadata = (from m in this._messagesMetadata where m.Number == messageType select m).SingleOrDefault();

            if (messageMetadata == null)
                throw new SshException(string.Format(CultureInfo.CurrentCulture, "Message type {0} is not valid.", messageType));

            var message = messageMetadata.Type.CreateInstance<PrivateKeyAgentMessage>();

            message.Load(data);

            this.Log(string.Format("ReceiveMessage from server: '{0}': '{1}'.", message.GetType().Name, message.ToString()));

            return message;
        }

        private void HandlePrivateKeyAgentMessage(RequestIdentitiesMessage message)
        {
            var identitiesAnswerMessage = new IdentitiesAnswerMessage(this.PrivateKeyAgent.ListSsh2());
            this.SendPrivateKeyAgentMessage(identitiesAnswerMessage);
        }

        private void HandlePrivateKeyAgentMessage(SignRequestMessage message)
        {
            var signResponseMessage = new SignResponseMessage(this.PrivateKeyAgent.SignSsh2(message.PublicKeyData, message.Data));
            this.SendPrivateKeyAgentMessage(signResponseMessage);
        }

        private void HandlePrivateKeyAgentMessage(AddIdentityMessage message)
        {
            bool success = this.PrivateKeyAgent.AddSsh2(message.Key, message.Comment) != null;
            this.SendSuccessMessage(success);
        }

        private void HandlePrivateKeyAgentMessage(RemoveIdentityMessage message)
        {
            bool success = this.PrivateKeyAgent.RemoveSsh2(message.PublicKeyData);
            this.SendSuccessMessage(success);
        }

        private void HandlePrivateKeyAgentMessage(RemoveAllIdentitiesMessage message)
        {
            this.PrivateKeyAgent.RemoveAllSsh2();
            this.SendPrivateKeyAgentMessage(new SuccessMessage());
        }

        private void HandlePrivateKeyAgentMessage(RequestRsaIdentitiesMessage message)
        {
            var rsaIdentitiesAnswerMessage = new RsaIdentitiesAnswerMessage(this.PrivateKeyAgent.ListSsh1());
            this.SendPrivateKeyAgentMessage(rsaIdentitiesAnswerMessage);
        }

        private void HandlePrivateKeyAgentMessage(RsaChallengeMessage message)
        {
            var signResponseMessage = new RsaResponseMessage(this.PrivateKeyAgent.DecryptSsh1(message.E, message.N, message.EncryptedChallenge, message.SessionId));
            this.SendPrivateKeyAgentMessage(signResponseMessage);
        }

        private void HandlePrivateKeyAgentMessage(AddRsaIdentityMessage message)
        {
            bool success = this.PrivateKeyAgent.AddSsh1(message.Key, message.Comment) != null;
            this.SendSuccessMessage(success);
        }

        private void HandlePrivateKeyAgentMessage(RemoveRsaIdentityMessage message)
        {
            bool success = this.PrivateKeyAgent.RemoveSsh1(message.E, message.N);
            this.SendSuccessMessage(success);
        }

        private void HandlePrivateKeyAgentMessage(RemoveAllRsaIdentitiesMessage message)
        {
            this.PrivateKeyAgent.RemoveAllSsh1();
            this.SendPrivateKeyAgentMessage(new SuccessMessage());
        }

        private void HandlePrivateKeyAgentMessage(PrivateKeyAgentMessage message)
        {
            this.SendPrivateKeyAgentMessage(new FailureMessage());
        }

        private void SendSuccessMessage(bool success)
        {
            if (success)
            {
                this.SendPrivateKeyAgentMessage(new SuccessMessage());
            }
            else
            {
                this.SendPrivateKeyAgentMessage(new FailureMessage());
            }
        }

        private static IEnumerable<PrivateKeyAgentMessageMetadata> GetPrivateKeyAgentMessagesMetadata()
        {
            return new PrivateKeyAgentMessageMetadata[] 
            { 
                // 3.1 Requests from client to agent for protocol 1 key operations
                new PrivateKeyAgentMessageMetadata { Name = "SSH_AGENTC_REQUEST_RSA_IDENTITIES", Number = 1, Type = typeof(RequestRsaIdentitiesMessage), },
                new PrivateKeyAgentMessageMetadata { Name = "SSH_AGENTC_RSA_CHALLENGE", Number = 3, Type = typeof(RsaChallengeMessage), },
                new PrivateKeyAgentMessageMetadata { Name = "SSH_AGENTC_ADD_RSA_IDENTITY", Number = 7, Type = typeof(AddRsaIdentityMessage), },
                new PrivateKeyAgentMessageMetadata { Name = "SSH_AGENTC_REMOVE_RSA_IDENTITY", Number = 8, Type = typeof(RemoveRsaIdentityMessage), },
                new PrivateKeyAgentMessageMetadata { Name = "SSH_AGENTC_REMOVE_ALL_RSA_IDENTITIES", Number = 9, Type = typeof(RemoveAllRsaIdentitiesMessage), },
                // 3.2 Requests from client to agent for protocol 2 key operations
                new PrivateKeyAgentMessageMetadata { Name = "SSH2_AGENTC_REQUEST_IDENTITIES", Number = 11, Type = typeof(RequestIdentitiesMessage), },
                new PrivateKeyAgentMessageMetadata { Name = "SSH2_AGENTC_SIGN_REQUEST", Number = 13, Type = typeof(SignRequestMessage), },
                new PrivateKeyAgentMessageMetadata { Name = "SSH2_AGENTC_ADD_IDENTITY", Number = 17, Type = typeof(AddIdentityMessage), },
                new PrivateKeyAgentMessageMetadata { Name = "SSH2_AGENTC_REMOVE_IDENTITY", Number = 18, Type = typeof(RemoveIdentityMessage), },
                new PrivateKeyAgentMessageMetadata { Name = "SSH2_AGENTC_REMOVE_ALL_IDENTITIES", Number = 19, Type = typeof(RemoveAllIdentitiesMessage), },
                // 3.3 Key-type independent requests from client to agent
                // 3.4 Generic replies from agent to client
                new PrivateKeyAgentMessageMetadata { Name = "SSH_AGENT_FAILURE", Number = 5, Type = typeof(FailureMessage), },
                new PrivateKeyAgentMessageMetadata { Name = "SSH_AGENT_SUCCESS", Number = 6, Type = typeof(SuccessMessage), },
                // 3.6 Replies from agent to client for protocol 2 key operations
                new PrivateKeyAgentMessageMetadata { Name = "SSH2_AGENT_IDENTITIES_ANSWER", Number = 12, Type = typeof(IdentitiesAnswerMessage), },
                new PrivateKeyAgentMessageMetadata { Name = "SSH2_AGENT_SIGN_RESPONSE", Number = 14, Type = typeof(SignResponseMessage), },
            };
        }

        private class PrivateKeyAgentMessageMetadata
        {
            public string Name { get; set; }

            public byte Number { get; set; }

            public Type Type { get; set; }
        }

        private void Log(string text)
        {
            Debug.WriteLine(text);
        }
    }
}
