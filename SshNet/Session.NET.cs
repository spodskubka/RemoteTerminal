using System.Linq;
using System;
using System.Net;
using Renci.SshNet.Messages;
using Renci.SshNet.Common;
using System.Threading;
using Renci.SshNet.Messages.Transport;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using Windows.Networking.Sockets;
using Windows.Networking;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Renci.SshNet
{
    public partial class Session
    {
        partial void SocketConnect(string host, int port)
        {
            this._socket = new StreamSocket();

            uint socketBufferSize = 2 * MAXIMUM_PACKET_SIZE;

            this._socket.Control.NoDelay = true;
            this._socket.Control.OutboundBufferSizeInBytes = socketBufferSize;

            this.Log(string.Format("Initiating connect to '{0}:{1}'.", this.ConnectionInfo.Host, this.ConnectionInfo.Port));

            //  Connect socket with specified timeout
            try
            {
                this._socket.ConnectAsync(new HostName(host), port.ToString()).AsTask().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new SshConnectionException(ex.Message, ex);
            }

            this._socketReader = new DataReader(this._socket.InputStream);
            this._socketWriter = new DataWriter(this._socket.OutputStream);
        }

        partial void SocketDisconnect()
        {
            this._socket.Dispose();
        }

        partial void SocketReadLine(ref string response)
        {
            var encoding = new Renci.SshNet.Common.ASCIIEncoding();

            var line = new StringBuilder();
            //  Read data one byte at a time to find end of line and leave any unhandled information in the buffer to be processed later
            var buffer = new List<byte>();

            var data = new byte[1];
            do
            {
                var received = this._socketReader.Receive(data);

                //  If zero bytes received then exit
                if (received == 0)
                    break;

                buffer.Add(data[0]);
            }
            while (!(buffer.Count > 1 && (buffer[buffer.Count - 1] == 0x0A || buffer[buffer.Count - 1] == 0x00)));

            // Return an empty version string if the buffer consists of a 0x00 character.
            if (buffer[buffer.Count - 1] == 0x00)
            {
                response = string.Empty;
            }
            else if (buffer.Count > 1 && buffer[buffer.Count - 2] == 0x0D)
                response = encoding.GetString(buffer.ToArray(), 0, buffer.Count - 2);
            else
                response = encoding.GetString(buffer.ToArray(), 0, buffer.Count - 1);
        }

        /// <summary>
        /// Function to read <paramref name="length"/> amount of data before returning, or throwing an exception.
        /// </summary>
        /// <param name="length">The amount wanted.</param>
        /// <param name="buffer">The buffer to read to.</param>
        /// <exception cref="SshConnectionException">Happens when the socket is closed.</exception>
        /// <exception cref="Exception">Unhandled exception.</exception>
        partial void SocketRead(int length, ref byte[] buffer)
        {
            var offset = 0;
            int receivedTotal = 0;  // how many bytes is already received

            do
            {
                try
                {
                    var receivedBytes = this._socketReader.Receive(buffer, offset + receivedTotal, length - receivedTotal);
                    if (receivedBytes > 0)
                    {
                        receivedTotal += receivedBytes;
                        continue;
                    }
                    else
                    {
                        // 2012-09-11: Kenneth_aa
                        // When Disconnect or Dispose is called, this throws SshConnectionException(), which...
                        // 1 - goes up to ReceiveMessage() 
                        // 2 - up again to MessageListener()
                        // which is where there is a catch-all exception block so it can notify event listeners.
                        // 3 - MessageListener then again calls RaiseError().
                        // There the exception is checked for the exception thrown here (ConnectionLost), and if it matches it will not call Session.SendDisconnect().
                        //
                        // Adding a check for this._isDisconnecting causes ReceiveMessage() to throw SshConnectionException: "Bad packet length {0}".
                        //
                        throw new SshConnectionException("An established connection was aborted by the software in your host machine.", DisconnectReason.ConnectionLost);
                    }
                }
                catch (Exception)
                {
                    /*if (exp.SocketErrorCode == SocketError.ConnectionAborted)
                    {
                        buffer = new byte[length];
                        this.Disconnect();
                        return;
                    }
                    else if (exp.SocketErrorCode == SocketError.WouldBlock ||
                       exp.SocketErrorCode == SocketError.IOPending ||
                       exp.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably empty, wait and try again
                        Task.Delay(30);
                    }
                    else*/
                    throw;  // any serious error occurred
                }
            } while (receivedTotal < length);
        }

        partial void SocketWrite(byte[] data)
        {
            int sent = 0;  // how many bytes is already sent
            int length = data.Length;

            do
            {
                try
                {
                    sent += this._socketWriter.Send(data, sent, length - sent);
                }
                catch (Exception)
                {
                    /*if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably full, wait and try again
                        Task.Delay(30);
                    }
                    else*/
                    throw;  // any serious error occurr
                }
            } while (sent < length);
        }

        partial void Log(string text)
        {
            Debug.WriteLine(text);
        }
    }
}
