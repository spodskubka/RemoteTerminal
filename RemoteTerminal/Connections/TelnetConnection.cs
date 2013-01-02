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
        private string command = string.Empty;

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
                this.reader = new StreamReader(socket.InputStream.AsStreamForRead(0), Encoding.GetEncoding("ASCII"));
                this.writer = new StreamWriter(socket.OutputStream.AsStreamForWrite(0), Encoding.GetEncoding("ASCII"));
                return true;
            }
            catch (Exception ex)
            {
                exception = ex.ToString();
            }

            terminal.WriteLine(exception);
            return false;
        }

        public async Task<string> ReadAsync()
        {
            this.CheckDisposed();
            this.MustBeConnected(true);

            char[] buffer = new char[4096];
            int read = await this.reader.ReadAsync(buffer, 0, buffer.Length);
            string input = new string(buffer, 0, read);
            return input;
        }

        public void Write(string str)
        {
            this.CheckDisposed();
            this.MustBeConnected(true);

            int pos;
            while ((pos = str.IndexOf("\r\n")) >= 0)
            {
                command += str.Substring(0, pos + 2);
                if (str.Length > pos + 2)
                {
                    str = str.Substring(pos + 2);
                }
                else
                {
                    str = string.Empty;
                }

                this.writer.Write(command);
                this.writer.Flush();

                command = string.Empty;
            }

            command += str;
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
