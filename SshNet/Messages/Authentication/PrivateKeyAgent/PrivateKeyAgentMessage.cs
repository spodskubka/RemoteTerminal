using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet.Messages;

namespace Renci.SshNet.Messages.Authentication.PrivateKeyAgent
{
    public abstract class PrivateKeyAgentMessage : Message
    {
        protected override void LoadData()
        {
            this.ReadUInt32();
        }

        protected override void SaveData()
        {
        }

        public override byte[] GetBytes()
        {
            var bytes = new List<byte>(base.GetBytes());
            uint messageLength = (uint)bytes.Count;
            bytes.InsertRange(0, messageLength.GetBytes());
            return bytes.ToArray();
        }
    }
}
