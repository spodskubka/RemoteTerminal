using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Renci.SshNet.Common
{
    public static partial class Extensions
    {
        /// <summary>
        /// Fills the passed byte array with data. Returns only when the byte array is full.
        /// </summary>
        /// <param name="dataReader">The <see cref="DataReader"/> from which to read the data.</param>
        /// <param name="buffer">The byte array that should be filled with data.</param>
        /// <returns>The amount of bytes read (must be equal to the length of the specified byte array).</returns>
        internal static int Receive(this DataReader dataReader, byte[] buffer)
        {
            return dataReader.Receive(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Fills the passed byte array with data. Returns only when the byte array is full.
        /// </summary>
        /// <param name="dataReader">The <see cref="DataReader"/> from which to read the data.</param>
        /// <param name="buffer">The byte array that should be filled with data.</param>
        /// <param name="offset">The location in <paramref name="buffer"/> to store the received data.</param>
        /// <param name="count">The number of bytes to receive.</param>
        /// <returns>The number of bytes received (must be equal to <paramref name="count"/>).</returns>
        internal static int Receive(this DataReader dataReader, byte[] buffer, int offset, int count)
        {
            var loadAsync = dataReader.LoadAsync((uint)count).AsTask();
            loadAsync.Wait();
            byte[] tempData = new byte[count];
            dataReader.ReadBytes(tempData);
            System.Buffer.BlockCopy(tempData, 0, buffer, offset, count);

            return (int)loadAsync.Result;
        }

        /// <summary>
        /// Sends the passed byte array.
        /// </summary>
        /// <param name="dataWriter">The <see cref="DataWriter"/> to which to send the data.</param>
        /// <param name="data">The byte array that should be sent.</param>
        /// <param name="offset">The location in <paramref name="data"/> at which to begin sending data.</param>
        /// <param name="count">The number of bytes to send.</param>
        /// <returns>The number of bytes sent (must be equal to <paramref name="count"/>).</returns>
        internal static int Send(this DataWriter dataWriter, byte[] data, int offset, int count)
        {
            byte[] tempData = new byte[count];
            System.Buffer.BlockCopy(data, offset, tempData, 0, count);
            dataWriter.WriteBytes(tempData);
            
            var storeAsync = dataWriter.StoreAsync().AsTask();
            storeAsync.Wait();

            return (int)storeAsync.Result;
        }

        internal static bool IsValidPort(this uint value)
        {
            if (value < 1)
                return false;

            if (value > 65535)
                return false;
            return true;
        }

        internal static bool IsValidPort(this int value)
        {
            if (value < 1)
                return false;

            if (value > 65535)
                return false;
            return true;
        }
    }
}
