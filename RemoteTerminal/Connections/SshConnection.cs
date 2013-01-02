using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;
using Renci.SshNet;
using Renci.SshNet.Common;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace RemoteTerminal.Connections
{
    internal class SshConnection : IConnection
    {
        private ConnectionData connectionData;

        private SshClient client;
        private ShellStream stream;
        private StreamReader reader;
        private StreamWriter writer;

        private bool isDisposed = false;

        public bool IsConnected
        {
            get
            {
                return this.stream != null && this.reader != null && this.writer != null;
            }
        }

        public void Initialize(ConnectionData connectionData)
        {
            this.CheckDisposed();
            this.MustBeConnected(false);

            if (connectionData.Type != ConnectionType.Ssh)
            {
                throw new ArgumentException("ConnectionData does not use Ssh connection.", "connectionData");
            }

            this.connectionData = connectionData;
        }

        public async Task<bool> ConnectAsync(IConnectionInitializingTerminal terminal)
        {
            this.CheckDisposed();
            this.MustBeConnected(false);

            string username = this.connectionData.Username;
            if (string.IsNullOrEmpty(username))
            {
                username = await terminal.ReadLineAsync("Username: ", echo: true);
            }
            else
            {
                terminal.WriteLine("Username: " + username);
            }

            string exception;
            int numRetries = 0;
            do
            {
                exception = null;
                bool retry = true;
                try
                {
                    ConnectionInfo connectionInfo;
                    switch (this.connectionData.Authentication)
                    {
                        case RemoteTerminal.Model.AuthenticationType.Password:
                            string password = await terminal.ReadLineAsync("Password: ", echo: false);
                            var passwordConnectionInfo = new PasswordConnectionInfo(this.connectionData.Host, this.connectionData.Port, username, password);
                            passwordConnectionInfo.PasswordExpired += (sender, e) =>
                            {
                                terminal.WriteLine("Password expired for user " + e.Username);
                                do
                                {
                                    var readNewPassword1Task = terminal.ReadLineAsync("New password: ", echo: false);
                                    readNewPassword1Task.Wait();
                                    string newPassword1 = readNewPassword1Task.Result;
                                    var readNewPassword2Task = terminal.ReadLineAsync("Repeat new password: ", echo: false);
                                    readNewPassword2Task.Wait();
                                    string newPassword2 = readNewPassword2Task.Result;

                                    if (newPassword1 == newPassword2)
                                    {
                                        e.NewPassword = newPassword1;
                                        break;
                                    }
                                }
                                while (true);
                            };
                            connectionInfo = passwordConnectionInfo;
                            break;
                        case RemoteTerminal.Model.AuthenticationType.KeyboardInteractive:
                            var keyboardInteractiveConnectionInfo = new KeyboardInteractiveConnectionInfo(this.connectionData.Host, this.connectionData.Port, username);
                            keyboardInteractiveConnectionInfo.AuthenticationPrompt += (sender, e) =>
                            {
                                if (e.Prompts.Count() > 0)
                                {
                                    terminal.WriteLine("Performing keyboard-interactive authentication.");
                                }

                                if (!string.IsNullOrEmpty(e.Instruction))
                                {
                                    terminal.WriteLine(e.Instruction);
                                }

                                foreach (var prompt in e.Prompts)
                                {
                                    var readLineTask = terminal.ReadLineAsync(prompt.Request, echo: prompt.IsEchoed);
                                    readLineTask.Wait();
                                    prompt.Response = readLineTask.Result;
                                }
                            };
                            connectionInfo = keyboardInteractiveConnectionInfo;
                            break;
                        case Model.AuthenticationType.PrivateKey:
                            PrivateKeyData privateKeyData = PrivateKeysDataSource.GetPrivateKey(connectionData.PrivateKeyName);
                            if (privateKeyData == null)
                            {
                                throw new Exception("Private Key '" + connectionData.PrivateKeyName + "' not found. Please correct the authentication details of the connection.");
                            }

                            PrivateKeyFile privateKey;
                            try
                            {
                                using (var privateKeyStream = new MemoryStream(privateKeyData.Data))
                                {
                                    privateKey = new PrivateKeyFile(privateKeyStream);
                                }
                            }
                            catch (SshPassPhraseNullOrEmptyException)
                            {
                                privateKey = null;
                            }

                            if (privateKey == null)
                            {
                                string privateKeyPassword = await terminal.ReadLineAsync("Private Key password: ", echo: false);
                                using (var privateKeyStream = new MemoryStream(privateKeyData.Data))
                                {
                                    try
                                    {
                                        privateKey = new PrivateKeyFile(privateKeyStream, privateKeyPassword);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new SshAuthenticationException("Wrong Private Key password, please try again.", ex);
                                    }
                                }
                            }

                            var privateKeyConnectionInfo = new PrivateKeyConnectionInfo(this.connectionData.Host, this.connectionData.Port, username, privateKey);
                            connectionInfo = privateKeyConnectionInfo;
                            break;
                        default:
                            throw new NotImplementedException("Authentication method '" + this.connectionData.Authentication + "' not implemented.");
                    }

                    connectionInfo.AuthenticationBanner += (sender, e) =>
                    {
                        terminal.WriteLine(e.BannerMessage.Replace("\n", "\r\n"));
                    };

                    this.client = new SshClient(connectionInfo);
                    await Task.Run(() => { this.client.Connect(); });

                    this.stream = this.client.CreateShellStream(terminal.TerminalName, (uint)terminal.Columns, (uint)terminal.Rows, 0, 0, 1024);

                    this.reader = new StreamReader(this.stream);
                    this.writer = new StreamWriter(this.stream);
                    this.writer.AutoFlush = true;
                    return true;
                }
                catch (SshConnectionException ex)
                {
                    exception = ex.Message;
                    retry = false;
                }
                catch (SshAuthenticationException ex)
                {
                    exception = ex.Message;
                    retry = true;
                }
                catch (Exception ex)
                {
                    exception = ex.Message;
                    retry = false;
                }

                if (exception != null)
                {
                    terminal.WriteLine(exception);
                    if (!retry || numRetries++ > 5)
                    {
                        return false;
                    }
                }
            }
            while (true);
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

            this.writer.Write(str);
            this.writer.Flush();
        }

        public void ResizeTerminal(int rows, int columns)
        {
            this.MustBeConnected(true);

            this.stream.ResizeTerminal((uint)columns, (uint)rows, 0, 0);
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

            if (this.stream != null)
            {
                this.stream.Dispose();
                this.stream = null;
            }

            if (this.client != null)
            {
                try
                {
                    this.client.Disconnect();
                }
                catch (NullReferenceException)
                {
                    // this can happen because the IsConnected code was not ported to WinRT perfectly
                }
                this.client.Dispose();
                this.client = null;
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
