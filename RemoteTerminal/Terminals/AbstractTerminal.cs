using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RemoteTerminal.Connections;
using RemoteTerminal.Model;
using RemoteTerminal.Screens;
using Windows.System;

namespace RemoteTerminal.Terminals
{
    public abstract class AbstractTerminal : IConnectionInitializingTerminal, IDisposable
    {
        private readonly IConnection connection = null;
        private readonly bool localEcho;
        private readonly string writtenNewLine;

        private IWritableScreen screen = null;
        private ManualResetEventSlim screenInitWaiter = new ManualResetEventSlim();

        private string localReadPrompt = string.Empty;
        private bool localReadEcho = false;
        private AutoResetEvent localReadSync = null;
        private string localReadLine = string.Empty;
        private int localReadStartColumn = 0;
        private int localReadStartRow = 0;

        public AbstractTerminal(ConnectionData connectionData, bool localEcho, string writtenNewLine)
        {
            switch (connectionData.Type)
            {
                case ConnectionType.Telnet:
                    this.connection = new TelnetConnection();
                    break;
                case ConnectionType.Ssh:
                    this.connection = new SshConnection();
                    break;
                default:
                    break;
            }

            this.connection.Initialize(connectionData);
            this.localEcho = localEcho;
            this.writtenNewLine = writtenNewLine;

            this.IsConnected = true;
        }

        public abstract string TerminalName { get; }

        public void WriteLine(string text)
        {
            using (var modifier = this.screen.GetModifier())
            {
                foreach (var ch in text)
                {
                    this.DrawLocalModeChar(ch, modifier, echo: true);
                }

                modifier.CursorColumn = 0;
                modifier.CursorRowIncreaseWithScroll(null, null);
            }
        }

        public async Task<string> ReadLineAsync(string prompt, bool echo)
        {
            this.localReadStartColumn = this.screen.CursorColumn;
            this.localReadStartRow = this.screen.CursorRow;
            this.localReadPrompt = prompt;
            this.localReadEcho = echo;

            using (var modifier = this.screen.GetModifier())
            {
                foreach (var ch in prompt)
                {
                    this.DrawLocalModeChar(ch, modifier, echo: true);
                }
            }

            using (this.localReadSync = new AutoResetEvent(false))
            {
                await Task.Run(() => this.localReadSync.WaitOne());
            }

            this.localReadSync = null;
            this.localReadPrompt = string.Empty;

            if (this.localReadLine == null)
            {
                throw new InvalidOperationException("Disconnecting during connection.");
            }

            string line = this.localReadLine;
            this.localReadLine = string.Empty;
            return line;
        }

        public IRenderableScreen RenderableScreen
        {
            get { return this.screen; }
        }

        public bool ScreenHasFocus
        {
            set
            {
                using (var modifier = this.screen.GetModifier())
                {
                    modifier.HasFocus = value;
                }
            }
        }

        public void PowerOn()
        {
            Task.Factory.StartNew(async () =>
            {
                this.screenInitWaiter.Wait();
                bool connected = await this.connection.ConnectAsync(this);
                if (connected)
                {
                    string str;
                    do
                    {
                        str = await this.connection.ReadAsync();
                        this.ProcessConnectionInput(str);
                    }
                    while (str.Length > 0);
                }

                this.IsConnected = false;
                var disconnected = this.Disconnected;
                if (disconnected != null)
                {
                    disconnected.Invoke(this, new EventArgs());
                }
            });
        }

        public void PowerOff()
        {
            this.connection.Disconnect();

            if (this.localReadSync != null)
            {
                this.localReadLine = null;
                this.localReadSync.Set();
            }
        }

        public void ResizeScreen(int rows, int columns)
        {
            if (this.screen == null)
            {
                this.screen = new Screen(rows, columns);
                this.screenInitWaiter.Set();
            }
            else
            {
                using (var modifier = this.screen.GetModifier())
                {
                    if (this.localReadSync != null)
                    {
                        modifier.Erase(this.localReadStartRow, this.localReadStartColumn, modifier.CursorRow, modifier.CursorColumn, null);
                        modifier.CursorRow = this.localReadStartRow;
                        modifier.CursorColumn = this.localReadStartColumn;
                    }

                    modifier.Resize(rows, columns);

                    if (this.localReadSync != null)
                    {
                        foreach (var localReadPromptChar in this.localReadPrompt)
                        {
                            this.DrawLocalModeChar(localReadPromptChar, modifier, echo: true);
                        }

                        foreach (var localReadInputChar in this.localReadLine)
                        {
                            this.DrawLocalModeChar(localReadInputChar, modifier, echo: this.localReadEcho);
                        }
                    }
                }

                if (this.connection.IsConnected)
                {
                    this.connection.ResizeTerminal(rows, columns);
                }
            }
        }

        public void ProcessKeyPress(char ch)
        {
            // This method receives all input that represents "characters".
            // It does not receive: Return, Cursor keys (Up, Down, Left, Right), Tabulator, Function keys (F1 - F12), 
            string str = ch.ToString();

            if (this.localReadSync != null)
            {
                if (str == "\b")
                {
                    if (this.localReadLine.Length == 0)
                    {
                        this.ProcessConnectionInput('\a');
                    }
                    else
                    {
                        this.localReadLine = this.localReadLine.Substring(0, this.localReadLine.Length - 1);

                        using (var modifier = this.screen.GetModifier())
                        {
                            modifier.Erase(this.localReadStartRow, this.localReadStartColumn, modifier.CursorRow, modifier.CursorColumn, null);
                            modifier.CursorRow = this.localReadStartRow;
                            modifier.CursorColumn = this.localReadStartColumn;

                            foreach (var localReadPromptChar in this.localReadPrompt)
                            {
                                this.DrawLocalModeChar(localReadPromptChar, modifier, echo: true);
                            }

                            foreach (var localReadInputChar in this.localReadLine)
                            {
                                this.DrawLocalModeChar(localReadInputChar, modifier, echo: this.localReadEcho);
                            }
                        }
                    }
                }
                else
                {
                    this.localReadLine += ch;
                    using (var modifier = this.screen.GetModifier())
                    {
                        this.DrawLocalModeChar(ch, modifier, echo: this.localReadEcho);
                    }
                }
            }
            else
            {
                this.ProcessUserInput(ch);
            }
        }

        private void DrawLocalModeChar(char ch, IScreenModifier modifier, bool echo)
        {
            modifier.CursorCharacter = echo ? ch : '●';
            if (modifier.CursorColumn + 1 >= this.Screen.ColumnCount)
            {
                modifier.CursorColumn = 0;
                modifier.CursorRowIncreaseWithScroll(scrollTop: null, scrollBottom: null);
            }
            else
            {
                modifier.CursorColumn++;
            }
        }

        public bool ProcessKeyPress(VirtualKey key, KeyModifiers keyModifiers)
        {
            if (this.localReadSync != null)
            {
                if (key == VirtualKey.Enter)
                {
                    using (var modifier = this.screen.GetModifier())
                    {
                        modifier.CursorColumn = 0;
                        modifier.CursorRowIncreaseWithScroll(null, null);
                    }
                    this.localReadSync.Set();
                }

                return true;
            }
            else
            {
                return this.ProcessUserInput(key, keyModifiers);
            }
        }

        protected abstract void ProcessUserInput(char ch);
        protected abstract bool ProcessUserInput(VirtualKey key, KeyModifiers keyModifiers);

        public bool IsConnected { get; private set; }
        public event EventHandler Disconnected;

        private void ProcessConnectionInput(string str)
        {
            foreach (var ch in str)
            {
                this.ProcessConnectionInput(ch);
            }
        }

        protected abstract void ProcessConnectionInput(char ch);

        protected IWritableScreen Screen
        {
            get
            {
                return this.screen;
            }
        }

        protected void Transmit(string str)
        {
            if (!this.IsConnected)
            {
                return;
            }

            if (this.localEcho)
            {
                this.ProcessConnectionInput(str == Environment.NewLine ? "\r\n" : str);
            }

            this.connection.Write(str == Environment.NewLine ? this.writtenNewLine : str);
        }

        public void Dispose()
        {
            this.screenInitWaiter.Dispose();

            if (this.connection != null)
            {
                this.connection.Dispose();
            }
        }

        public int Rows
        {
            get { return this.Screen.RowCount; }
        }

        public int Columns
        {
            get { return this.Screen.ColumnCount; }
        }
    }
}
