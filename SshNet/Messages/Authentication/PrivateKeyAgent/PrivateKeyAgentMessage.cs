using System;
using System.Collections.Generic;
using System.Linq;
using Renci.SshNet.Common;

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

        /// <summary>
        /// Reads next mpint1 (SSH1) data type from internal buffer.
        /// </summary>
        /// <returns>mpint read.</returns>
        protected BigInteger ReadBigInt1()
        {
            var bitLength = this.ReadUInt16();

            var data = this.ReadBytes((int)((int)bitLength + 7) / 8);

            return new BigInteger(data);
        }

        /// <summary>
        /// Reads next mpint1 (SSH1) data type from internal buffer.
        /// </summary>
        /// <returns>mpint read.</returns>
        protected void WriteBigInt1(BigInteger bigInt)
        {
            this.Write((UInt16)bigInt.BitLength);

            var bytes = bigInt.ToByteArray().Reverse();
            this.Write(bytes);
        }
    }
}
