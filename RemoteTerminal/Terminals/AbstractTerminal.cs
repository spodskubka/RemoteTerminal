// Remote Terminal, an SSH/Telnet terminal emulator for Microsoft Windows
// Copyright (C) 2012-2015 Stefan Podskubka
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RemoteTerminal.Connections;
using RemoteTerminal.Model;
using RemoteTerminal.Screens;
using Windows.System;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// This is an abstract implementation of a terminal. It should be used as a base class for specialized terminal implementations.
    /// </summary>
    /// <remarks>
    /// Basically a terminal has
    ///  * a screen (<see cref="IWritableScreen"/>)
    ///  * an input device (keyboard)
    ///  * the ability to connect somewhere (<see cref="ConnectionData"/>, <see cref="TelnetConnection"/>, <see cref="SshConnection"/>)
    /// and can be powered on and off.
    /// This class implements basic handling of screen display, user input and network in-/output.
    /// </remarks>
    public abstract class AbstractTerminal : IConnectionInitializingTerminal, IDisposable
    {
        /// <summary>
        /// The synchronization context for events.
        /// </summary>
        /// <remarks>
        /// This is needed for the implementation of <see cref="INotifyPropertyChanged"/>.
        /// </remarks>
        private SynchronizationContext synchronizationContext;

        /// <summary>
        /// The connection data of this terminal.
        /// </summary>
        private readonly ConnectionData connectionData;

        /// <summary>
        /// The new-line characters that are written to the connection when the user presses Return/Enter.
        /// </summary>
        private readonly string writtenNewLine;

        /// <summary>
        /// The active connection of this terminal.
        /// </summary>
        private IConnection connection = null;

        /// <summary>
        /// The active screen of this terminal (null if none is assigned).
        /// </summary>
        private IWritableScreen screen = null;

        /// <summary>
        /// A synchronization object used to wait for screen initialization when powering on the terminal.
        /// </summary>
        private ManualResetEventSlim screenInitWaiter = new ManualResetEventSlim();

        /// <summary>
        /// The prompt for reading user input during connection initialization.
        /// </summary>
        private string localReadPrompt = string.Empty;

        /// <summary>
        /// A value indicating whether to echo user input in plain text during connection initialization (<see cref="false"/> for password input with * characters).
        /// </summary>
        private bool localReadEcho = false;

        /// <summary>
        /// A synchronization object used to wait for user input during connection initialization.
        /// </summary>
        private AutoResetEvent localReadSync = null;

        /// <summary>
        /// The user input read during connection initialization.
        /// </summary>
        private string localReadLine = string.Empty;

        /// <summary>
        /// The start column for reading user input during connection initialization.
        /// </summary>
        private int localReadStartColumn = 0;

        /// <summary>
        /// The start row for reading user input during connection initialization.
        /// </summary>
        private int localReadStartRow = 0;

        /// <summary>
        /// A value indicating whether the terminal is connected.
        /// </summary>
        private bool connected = false;

        /// <summary>
        /// A lock object used to prevent race-conditions when disconnecting.
        /// </summary>
        private object disconnectLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractTerminal"/> class.
        /// </summary>
        /// <param name="connectionData">The connection data.</param>
        /// <param name="localEcho">A value indicating whether all user input should be echoed on the terminal screen (when the server doesn't return the input to the terminal).</param>
        /// <param name="writtenNewLine">The new-line characters that are written to the connection when the user presses Return/Enter.</param>
        public AbstractTerminal(ConnectionData connectionData, bool localEcho, string writtenNewLine)
        {
            this.connectionData = connectionData;
            this.LocalEcho = localEcho;
            this.writtenNewLine = writtenNewLine;

            this.Name = connectionData.Name;
            this.Title = string.Empty;
            this.synchronizationContext = SynchronizationContext.Current;
        }

        /// <summary>
        /// Gets the name of the terminal implementation (e.g. dumb, vt100, xterm).
        /// </summary>
        /// <remarks>
        /// In case of an SSH connection this may be sent to the SSH server
        /// </remarks>
        public abstract string TerminalName { get; }

        /// <summary>
        /// Gets or sets a value indicating whether all user input should be echoed on the terminal screen (when the server doesn't return the input to the terminal).
        /// </summary>
        protected bool LocalEcho { get; set; }

        /// <summary>
        /// The (display) name of the terminal (comes from the connection data).
        /// </summary>
        private string name;

        /// <summary>
        /// Gets the (display) name of the terminal (comes from the connection data).
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }

            protected set
            {
                if (value != this.name)
                {
                    this.name = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The (display) title of the terminal as defined by the server.
        /// </summary>
        private string title;

        /// <summary>
        /// Gets the (display) title of the terminal as defined by the server.
        /// </summary>
        public string Title
        {
            get
            {
                return this.title;
            }

            protected set
            {
                if (value != this.title)
                {
                    this.title = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <remarks>
        /// This is needed for the implementation of <see cref="INotifyPropertyChanged"/>.
        /// This method is called by the Set accessor of each property. 
        /// The CallerMemberName attribute that is applied to the optional propertyName 
        /// parameter causes the property name of the caller to be substituted as an argument. 
        /// </remarks>
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var propertyChanged = this.PropertyChanged;
            if (propertyChanged != null)
            {
                this.synchronizationContext.Post(state => propertyChanged(this, new PropertyChangedEventArgs(propertyName)), null);
            }
        }

        /// <summary>
        /// Writes a line of text to the terminal while no connection is established (e.g. for connection initialization or disconnect notification).
        /// </summary>
        /// <param name="text">The line of text to write to the terminal.</param>
        /// <exception cref="InvalidOperationException">A connection is established at the moment.</exception>
        public void WriteLine(string text)
        {
            if (this.connected)
            {
                throw new InvalidOperationException("This method can only be called before the connection is established.");
            }

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

        /// <summary>
        /// Reads a line of user input while no connection is established (e.g. for connection initialization).
        /// </summary>
        /// <param name="prompt">A prompt to display (<see cref="string.Empty"/> for no prompt).</param>
        /// <param name="echo">A value indicating whether to echo the user input back to the terminal in plain text (<see cref="false"/> for password input with * characters).</param>
        /// <returns>The read line of user input (without a new-line at the end).</returns>
        /// <exception cref="InvalidOperationException">A connection is established at the moment.</exception>
        public async Task<string> ReadLineAsync(string prompt, bool echo)
        {
            if (this.connected)
            {
                throw new InvalidOperationException("This method can only be called before the connection is established.");
            }

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

        /// <summary>
        /// Gets the renderable screen associated with this terminal.
        /// </summary>
        public IRenderableScreen RenderableScreen
        {
            get { return this.screen; }
        }

        /// <summary>
        /// Sets a value indicating whether the screen of this terminal has focus or not (may result in a differently drawn cursor).
        /// </summary>
        public bool ScreenHasFocus
        {
            set
            {
                if (this.screen != null)
                {
                    using (var modifier = this.screen.GetModifier())
                    {
                        modifier.HasFocus = value && this.IsConnected;
                    }
                }
            }
        }

        /// <summary>
        /// Turns on this terminal and initializes the connection.
        /// </summary>
        public void PowerOn()
        {
            Task.Factory.StartNew(async () =>
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
                this.IsConnected = true;

                this.screenInitWaiter.Wait();
                bool connected = await this.connection.ConnectAsync(this);
                if (connected)
                {
                    this.connected = true;

                    try
                    {
                        string str;
                        do
                        {
                            str = await this.connection.ReadAsync();
                            this.ProcessConnectionInput(str);
                        }
                        while (str.Length > 0);
                    }
                    catch (Exception)
                    {
                    }
                }

                this.PowerOff();
            });
        }

        /// <summary>
        /// Disconnects the connection and turns off the terminal.
        /// </summary>
        /// <param name="ex">The exception that is the reason for the power of, if any.</param>
        public void PowerOff(Exception ex = null)
        {
            this.IsConnected = false;
            this.ScreenHasFocus = false;
            this.connected = false;

            lock (this.disconnectLock)
            {
                this.connection.Disconnect();
                this.connection.Dispose();
                this.connection = null;
            }

            if (this.localReadSync != null)
            {
                this.localReadLine = null;
                this.localReadSync.Set();
            }

            var disconnected = this.Disconnected;
            if (disconnected != null)
            {
                disconnected.Invoke(this, new EventArgs());
            }

            if (ex != null)
            {
                this.WriteLine(string.Empty);
                this.WriteLine(ex.Message);
            }

            this.WriteLine(string.Empty);
            this.WriteLine("Press Ctrl+R to reconnect.");
        }

        /// <summary>
        /// Resizes the screen of the terminal to the specified screen size.
        /// </summary>
        /// <param name="rows">The amount of rows on the screen.</param>
        /// <param name="columns">The amount of columns on the screen.</param>
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

                IConnection connection = this.connection;
                if (connection != null && connection.IsConnected)
                {
                    connection.ResizeTerminal(rows, columns);
                }
            }
        }

        /// <summary>
        /// Processes a user's key press.
        /// </summary>
        /// <param name="ch">The input character.</param>
        public void ProcessKeyPress(char ch)
        {
            // This method receives all input that represents "characters".
            // It does not receive: Return, Cursor keys (Up, Down, Left, Right), Tabulator, Function keys (F1 - F12), Alt/Ctrl key combinations
            if (this.localReadSync != null)
            {
                if (ch == '\b')
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
                if (this.connected)
                {
                    this.ProcessUserInput(ch.ToString());
                }
                else
                {
                    Debug.WriteLine("Input character '" + ch + "' ignored, not yet connected.");
                }
            }
        }

        /// <summary>
        /// Draws a character to the screen while no connection is established.
        /// </summary>
        /// <param name="ch">The character to draw.</param>
        /// <param name="modifier">The existing screen modifier.</param>
        /// <param name="echo">A value indicating whether to draw the character to the terminal in plain text (<see cref="false"/> for password input with * characters).</param>
        private void DrawLocalModeChar(char ch, IScreenModifier modifier, bool echo)
        {
            ScreenCellFormat defaultFormat = new ScreenCellFormat();
            switch (ch)
            {
                case '\r':
                    modifier.CursorColumn = 0;
                    break;
                case '\n':
                    modifier.CursorRowIncreaseWithScroll(null, null);
                    break;
                default:
                    modifier.CursorCharacter = echo ? ch : '*';
                    modifier.ApplyFormatToCursor(defaultFormat);
                    if (modifier.CursorColumn + 1 >= this.Screen.ColumnCount)
                    {
                        modifier.CursorColumn = 0;
                        modifier.CursorRowIncreaseWithScroll(scrollTop: null, scrollBottom: null);
                    }
                    else
                    {
                        modifier.CursorColumn++;
                    }

                    break;
            }
        }

        /// <summary>
        /// Processes a user's key press.
        /// </summary>
        /// <param name="key">The pressed key.</param>
        /// <param name="keyModifiers">The key modifiers.</param>
        /// <returns>A value indicating whether the key press was processed.</returns>
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
                if (this.connected)
                {
                    return this.ProcessUserInput(key, keyModifiers);
                }
                else
                {
                    if (key == VirtualKey.R && keyModifiers == KeyModifiers.Ctrl)
                    {
                        this.WriteLine("Reconnecting...");
                        this.WriteLine(string.Empty);

                        this.PowerOn();
                    }

                    Debug.WriteLine("Input key '" + key + "' with modifier(s) '" + keyModifiers + "' ignored, not yet connected.");
                    return true;
                }
            }
        }

        /// <summary>
        /// Processes text that is pasted to the terminal.
        /// </summary>
        /// <param name="str">The pasted text.</param>
        public void ProcessPastedText(string str)
        {
            if (this.localReadSync != null)
            {
                string pastedText = str;
                int newlinePos = str.IndexOfAny(new[] { '\r', '\n' });
                if (newlinePos >= 0)
                {
                    pastedText = str.Substring(0, newlinePos);
                }

                foreach (char ch in pastedText)
                {
                    this.ProcessKeyPress(ch);
                }

                if (newlinePos >= 0)
                {
                    this.ProcessKeyPress(VirtualKey.Enter, KeyModifiers.None);
                }
            }
            else
            {
                this.ProcessUserInput(str.Replace("\r\n", "\r"));
            }
        }

        /// <summary>
        /// Processes user input in a way that is specific to the terminal implementation.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <remarks>
        /// This method receives all input that represents "characters".
        /// It does not receive: Return, Cursor keys (Up, Down, Left, Right), Tabulator, Function keys (F1 - F12), Alt/Ctrl key combinations
        /// </remarks>
        protected abstract void ProcessUserInput(string str);

        /// <summary>
        /// Processes user input in a way that is specific to the terminal implementation.
        /// </summary>
        /// <param name="key">The input key.</param>
        /// <param name="keyModifiers">The key modifiers.</param>
        /// <returns>A value indicating whether the key press was processed by the terminal implementation.</returns>
        /// <remarks>
        /// This method receives key presses of non-character keys (e.g. Up, Down, Left, Right, Function keys F1-F12, Alt/Ctrl key combinations, ...).
        /// </remarks>
        protected abstract bool ProcessUserInput(VirtualKey key, KeyModifiers keyModifiers);

        /// <summary>
        /// Gets a value indicating whether the terminal is connected.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Occurs when the terminal's connection is disconnected.
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// Processes input from the connection (sent by the server).
        /// </summary>
        /// <param name="str">The received string.</param>
        private void ProcessConnectionInput(string str)
        {
            foreach (var ch in str)
            {
                this.ProcessConnectionInput(ch);
            }
        }

        /// <summary>
        /// Processes input from the connection (sent by the server) in a way that is specific to the terminal implementation.
        /// </summary>
        /// <param name="ch">The received character.</param>
        protected abstract void ProcessConnectionInput(char ch);

        /// <summary>
        /// Gets the writable screen associated with this terminal.
        /// </summary>
        protected IWritableScreen Screen
        {
            get
            {
                return this.screen;
            }
        }

        /// <summary>
        /// Transmits something via the connection (to the server).
        /// </summary>
        /// <param name="str">The string to transmit.</param>
        protected void Transmit(string str)
        {
            if (!this.IsConnected)
            {
                return;
            }

            if (this.LocalEcho)
            {
                this.ProcessConnectionInput(str == Environment.NewLine ? "\r\n" : str);
            }

            try
            {
                IConnection connection = this.connection;
                if (connection != null)
                {
                    connection.Write(str == Environment.NewLine ? this.writtenNewLine : str);
                }
            }
            catch (Exception exception)
            {
                this.PowerOff(exception);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.screenInitWaiter.Dispose();

            if (this.connection != null)
            {
                this.connection.Dispose();
            }
        }

        /// <summary>
        /// Gets the amount of rows on the terminal screen.
        /// </summary>
        public int Rows
        {
            get { return this.Screen.RowCount; }
        }

        /// <summary>
        /// Gets the amount of columns on the terminal screen.
        /// </summary>
        public int Columns
        {
            get { return this.Screen.ColumnCount; }
        }
    }
}
