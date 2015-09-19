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
    internal class TelnetConnection : IConnection
    {
        private ConnectionData connectionData;
        private StreamSocket socket;
        private StreamReader reader;
        private StreamWriter writer;

        private bool isDisposed = false;

        public bool IsConnected
        {
            get
            {
                return this.socket != null && this.reader != null && this.writer != null;
            }
        }

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

        public void Write(string str)
        {
            this.CheckDisposed();
            this.MustBeConnected(true);

            this.writer.Write(str);
            this.writer.Flush();
        }

        public void ResizeTerminal(int rows, int columns)
        {
            // A telnet connection is unaffected by a terminal resize.
        }

        public void Disconnect()
        {
            this.Dispose();
        }

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

        private void CheckDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        private void MustBeConnected(bool connected)
        {
            if (connected != this.IsConnected)
            {
                throw new InvalidOperationException(connected ? "Not yet connected." : "Already connected.");
            }
        }
    }
}
