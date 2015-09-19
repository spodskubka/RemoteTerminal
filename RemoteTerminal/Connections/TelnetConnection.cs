using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace RemoteTerminal.Connections
{
    /// <summary>
    /// Represents a connection (more specific: a shell connection) to a Telnet server.
    /// </summary>
    internal class TelnetConnection : IConnection
    {
        /// <summary>
        /// The connection data for the connection.
        /// </summary>
        private ConnectionData connectionData;

        /// <summary>
        /// The TCP connection to the Telnet server.
        /// </summary>
        private StreamSocket socket;

        /// <summary>
        /// The reader for the TCP connection input stream.
        /// </summary>
        private StreamReader reader;

        /// <summary>
        /// The writer for the TCP connection output stream.
        /// </summary>
        private StreamWriter writer;

        /// <summary>
        /// A value indicating whether the current object has been disposed.
        /// </summary>
        private bool isDisposed = false;

        /// <summary>
        /// Gets a value indicating whether the connection object is actually connected to a server.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return this.socket != null && this.reader != null && this.writer != null;
            }
        }

        /// <summary>
        /// Initializes the connection object with the specified connection data.
        /// </summary>
        /// <param name="connectionData">The connection data for the connection.</param>
        /// <exception cref="ObjectDisposedException">The connection object is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The connection object is currently connected.</exception>
        /// <exception cref="ArgumentException">The <paramref name="connectionData"/> object contains a connection type that is not supported by the connection object.</exception>
        /// <exception cref="Exception">Some other error occured.</exception>
        public void Initialize(ConnectionData connectionData)
        {
            this.CheckDisposed();
            this.MustBeConnected(false);

            if (connectionData.Type != ConnectionType.Telnet)
            {
                throw new ArgumentException("ConnectionData does not use Telnet connection.", "connectionData");
            }

            this.connectionData = connectionData;
        }

        /// <summary>
        /// Establishes the connection to the server, using the specified <paramref name="terminal"/> for connection initialization (authentication, etc.).
        /// </summary>
        /// <param name="terminal">The terminal to use for connection initialization.</param>
        /// <returns>A value indicating whether the connection was successfully established.</returns>
        /// <exception cref="ObjectDisposedException">The connection object is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The connection object is currently connected.</exception>
        public async Task<bool> ConnectAsync(IConnectionInitializingTerminal terminal)
        {
            this.CheckDisposed();
            this.MustBeConnected(false);

            string exception = null;
            try
            {
                this.socket = new StreamSocket();
                await this.socket.ConnectAsync(new HostName(this.connectionData.Host), this.connectionData.Port.ToString());
                this.reader = new StreamReader(socket.InputStream.AsStreamForRead(0), Encoding.GetEncoding("ISO-8859-1"));
                this.writer = new StreamWriter(socket.OutputStream.AsStreamForWrite(0), Encoding.GetEncoding("ISO-8859-1"));
                return true;
            }
            catch (Exception ex)
            {
                exception = ex.ToString();
            }

            terminal.WriteLine(exception);
            return false;
        }

        private const char SE = (char)240;
        private const char SB = (char)250;
        private const char WILL = (char)251;
        private const char WONT = (char)252;
        private const char DO = (char)253;
        private const char DONT = (char)254;
        private const char IAC = (char)255;

        /// <summary>
        /// Reads a string from the server.
        /// </summary>
        /// <returns>The read string.</returns>
        /// <exception cref="ObjectDisposedException">The connection object is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The connection object is currently not connected.</exception>
        public async Task<string> ReadAsync()
        {
            this.CheckDisposed();
            this.MustBeConnected(true);

            StringBuilder input;
            int read;
            char[] buffer = new char[4096];
            do
            {
                read = await this.reader.ReadAsync(buffer, 0, buffer.Length);

                // Perform the DO/DON'T negotiation ignoring every request for now
                input = new StringBuilder(read);
                string nvtNegotiation = null;
                for (int i = 0; i < read; i++)
                {
                    if (nvtNegotiation == null)
                    {
                        if (buffer[i] == IAC)
                        {
                            nvtNegotiation = string.Empty;
                        }
                        else
                        {
                            input.Append(buffer[i]);
                        }
                    }
                    else if (nvtNegotiation.Length == 0)
                    {
                        if (buffer[i] == IAC)
                        {
                            input.Append(buffer[i]);
                            nvtNegotiation = null;
                        }
                        else if (buffer[i] == WILL  || buffer[i]==WONT || buffer[i] == DO || buffer[i] == DONT)
                        {
                            nvtNegotiation += buffer[i];
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        if (nvtNegotiation[0] == SB)
                        {
                            if (buffer[i] != SE)
                            {
                            }
                            else
                            {
                                nvtNegotiation = null;
                            }
                        }
                        else
                        {
                            nvtNegotiation += buffer[i];
                            if (nvtNegotiation[0] == WILL || nvtNegotiation[0] == WONT)
                            {
                                nvtNegotiation = null;
                            }
                            else
                            {
                                string answer = String.Concat(IAC, (char)WONT, nvtNegotiation[1]);
                                this.writer.Write(answer);
                                this.writer.Flush();
                                nvtNegotiation = null;
                            }
                        }
                    }
                }
            }
            while (input.Length == 0 && read > 0);

            return input.ToString();
        }

        /// <summary>
        /// Writes a string to the server.
        /// </summary>
        /// <param name="str">The string to write.</param>
        /// <exception cref="ObjectDisposedException">The connection object is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The connection object is currently not connected.</exception>
        public void Write(string str)
        {
            this.CheckDisposed();
            this.MustBeConnected(true);

            this.writer.Write(str);
            this.writer.Flush();
        }

        /// <summary>
        /// Indicates to the server that the terminal size has changed to the specified dimensions.
        /// </summary>
        /// <param name="rows">The new amount of rows.</param>
        /// <param name="columns">The new amount of columns.</param>
        /// <exception cref="InvalidOperationException">The connection object is currently not connected.</exception>
        public void ResizeTerminal(int rows, int columns)
        {
            // A telnet connection is unaffected by a terminal resize.
        }

        /// <summary>
        /// Closes the connection to the server.
        /// </summary>
        public void Disconnect()
        {
            this.Dispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            if (this.reader != null)
            {
                this.reader.Dispose();
                this.reader = null;
            }

            if (this.writer != null)
            {
                this.writer.Dispose();
                this.writer = null;
            }

            if (this.socket != null)
            {
                this.socket.Dispose();
                this.socket = null;
            }

            this.isDisposed = true;

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Checks whether the current object is disposed, in which case an <see cref="ObjectDisposedException"/> is thrown.
        /// </summary>
        private void CheckDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        /// <summary>
        /// Checks whether a specific connection state is not satisfied, in which case an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="connected"><see cref="true"/> if the connection should be connected to prevent an exception, <see cref="false"/> otherwise.</param>
        private void MustBeConnected(bool connected)
        {
            if (connected != this.IsConnected)
            {
                throw new InvalidOperationException(connected ? "Not yet connected." : "Already connected.");
            }
        }
    }
}
