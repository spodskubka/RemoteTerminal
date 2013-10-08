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
        private PrivateKeyData privateKeyData;

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

            // This is already done here instead of in the ConnectAsync method because here the PrivateKeysDataSource.GetPrivateKey method is able to access the Resources.
            if (connectionData.Authentication == AuthenticationType.PrivateKey)
            {
                this.privateKeyData = PrivateKeysDataSource.GetPrivateKey(connectionData.PrivateKeyName);
                if (this.privateKeyData == null)
                {
                    throw new Exception("Private Key '" + connectionData.PrivateKeyName + "' not found. Please correct the authentication details of the connection.");
                }
            }
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

            int numRetries = 0;
            do
            {
                bool retry = true;
                try
                {
                    ConnectionInfo connectionInfo;
                    PrivateKeyAgent forwardedPrivateKeyAgent = null;
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
                            PrivateKeyFile privateKey;

                            if (this.privateKeyData == null)
                            {
                                throw new Exception("Private Key '" + connectionData.PrivateKeyName + "' not found. Please correct the authentication details of the connection.");
                            }

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

                            // In the normal PrivateKey authentication there is only a connection-local PrivateKeyAgent.
                            var localPprivateKeyAgent = new PrivateKeyAgent();
                            localPprivateKeyAgent.AddSsh2(privateKey.HostKey, connectionData.PrivateKeyName);
                            var privateKeyConnectionInfo = new PrivateKeyConnectionInfo(this.connectionData.Host, this.connectionData.Port, username, localPprivateKeyAgent);
                            connectionInfo = privateKeyConnectionInfo;

                            break;
                        case AuthenticationType.PrivateKeyAgent:
                            var globalPrivateKeyAgent = PrivateKeyAgentManager.PrivateKeyAgent;
                            if (globalPrivateKeyAgent.ListSsh2().Count == 0)
                            {
                                throw new SshAuthenticationException("The private key agent doesn't contain any private keys.");
                            }

                            terminal.WriteLine("Performing private key agent authentication.");

                            var privateKeyAgentConnectionInfo = new PrivateKeyConnectionInfo(this.connectionData.Host, this.connectionData.Port, username, globalPrivateKeyAgent);
                            connectionInfo = privateKeyAgentConnectionInfo;
                            if (connectionData.PrivateKeyAgentForwarding == true)
                            {
                                forwardedPrivateKeyAgent = globalPrivateKeyAgent;
                                terminal.WriteLine("Agent forwarding is enabled.");
                            }

                            break;
                        default:
                            throw new NotImplementedException("Authentication method '" + this.connectionData.Authentication + "' not implemented.");
                    }

                    connectionInfo.AuthenticationBanner += (sender, e) =>
                    {
                        terminal.WriteLine(e.BannerMessage.Replace("\n", "\r\n"));
                    };

                    this.client = new SshClient(connectionInfo);
                    this.client.HostKeyReceived += (s, e) =>
                    {
                        string fingerprint = string.Join(":", e.FingerPrint.Select(b => b.ToString("x2")));

                        bool trustHostKey = true;
                        bool storeHostKey = false;

                        string oldHostKey = "a"+HostKeysDataSource.GetHostKey(this.connectionData.Host, this.connectionData.Port);
                        string newHostKey = string.Join(null, e.HostKey.Select(b => b.ToString("x2")));
                        if (oldHostKey == null)
                        {
                            terminal.WriteLine("Remote Terminal has not yet cached a host key for this server.");
                            terminal.WriteLine("Host key's fingerprint: " + fingerprint);
                            terminal.WriteLine("Please make sure the fingerprint matches the server's actual host key.");
                            trustHostKey = QueryYesNo(terminal, "Do you want to continue connecting to the host?");
                            if (trustHostKey)
                            {
                                storeHostKey = QueryYesNo(terminal, "Do you want to store this host key in the cache?");
                            }
                        }
                        else if (oldHostKey != newHostKey)
                        {
                            terminal.WriteLine("POSSIBLE SECURITY BREACH DETECTED!");
                            terminal.WriteLine("Remote Terminal has cached another host key for this server.");
                            terminal.WriteLine("This could mean one of two things:");
                            terminal.WriteLine(" * the server's host key was changed by an administrator");
                            terminal.WriteLine(" * another computer is trying to intercept your connection");
                            terminal.WriteLine("Host key's new fingerprint: " + fingerprint);
                            trustHostKey = QueryYesNo(terminal, "Do you want to continue connecting to the host?");
                            if (trustHostKey)
                            {
                                storeHostKey = QueryYesNo(terminal, "Do you want to update the cache with the new host key?");
                            }
                        }

                        e.CanTrust = trustHostKey;
                        if (storeHostKey)
                        {
                            HostKeysDataSource.AddOrUpdate(this.connectionData.Host, this.connectionData.Port, newHostKey);
                        }
                    };

                    this.client.ConnectionInfo.Timeout = new TimeSpan(0, 5, 0);
                    await Task.Run(() => { this.client.Connect(); });
                    this.client.ConnectionInfo.Timeout = new TimeSpan(0, 1, 0);

                    var terminalModes = new Dictionary<TerminalModes, uint>();
                    terminalModes[TerminalModes.TTY_OP_ISPEED] = 0x00009600;
                    terminalModes[TerminalModes.TTY_OP_OSPEED] = 0x00009600;
                    this.stream = this.client.CreateShellStream(terminal.TerminalName, (uint)terminal.Columns, (uint)terminal.Rows, 0, 0, 1024, forwardedPrivateKeyAgent, terminalModes.ToArray());

                    this.reader = new StreamReader(this.stream);
                    this.writer = new StreamWriter(this.stream);
                    this.writer.AutoFlush = true;
                    return true;
                }
                catch (SshConnectionException ex)
                {
                    terminal.WriteLine(ex.Message);
                    retry = false;
                }
                catch (SshAuthenticationException ex)
                {
                    terminal.WriteLine(ex.Message);
                    if (connectionData.Authentication == AuthenticationType.PrivateKeyAgent)
                    {
                        terminal.WriteLine("Please load the necessary private key(s) into the private key agent.");
                        retry = false;
                    }
                    else
                    {
                        retry = true;
                    }
                }
                catch (Exception ex)
                {
                    terminal.WriteLine(ex.Message);
                    retry = false;
                }

                if (!retry || numRetries++ > 5)
                {
                    return false;
                }
            }
            while (true);
        }

        private static bool QueryYesNo(IConnectionInitializingTerminal terminal, string prompt)
        {
            do
            {
                var readLineTask = terminal.ReadLineAsync(prompt + " [Y/n] ", echo: true);
                readLineTask.Wait();
                string keyConfirmationResult = readLineTask.Result.ToLowerInvariant();
                switch (keyConfirmationResult)
                {
                    case "":
                    case "y":
                        return true;
                    case "n":
                        return false;
                    default:
                        terminal.WriteLine("Invalid response.");
                        break;
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

            try
            {
                this.stream.ResizeTerminal((uint)columns, (uint)rows, 0, 0);
            }
            catch (Exception ex)
            {
                // The connection was probably disconnected.
                // Ignore it for now, it will be detected somewhere else.
            }
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
                catch (Exception)
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
