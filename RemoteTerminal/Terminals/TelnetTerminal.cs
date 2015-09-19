using System;
using RemoteTerminal.Model;
using Windows.System;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// This is a concrete terminal implementation of a dumb terminal, which doesn't process any control sequences at all.
    /// </summary>
    /// <remarks>
    /// The class is called TelnetTerminal just because it was used with the <see cref="TelnetConnection"/> class in the beginning.
    /// However, due to the <see cref="SshTerminal"/> class being used for all connections now, it isn't used at the moment.
    /// </remarks>
    public class TelnetTerminal : AbstractTerminal, ITerminal, IConnectionInitializingTerminal
    {
        /// <summary>
        /// The default tab stop width.
        /// </summary>
        private const int DefaultTabStopWidth = 8;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelnetTerminal"/> class with the specified connection data.
        /// </summary>
        /// <param name="connectionData">The connection data.</param>
        public TelnetTerminal(ConnectionData connectionData)
            : base(connectionData, localEcho: true, writtenNewLine: "\r\n")
        {
        }

        /// <summary>
        /// Gets the name of the terminal implementation (e.g. dumb, vt100, xterm).
        /// </summary>
        /// <remarks>
        /// In case of an SSH connection this may be sent to the SSH server
        /// </remarks>
        public override string TerminalName
        {
            get { return "telnet"; }
        }

        /// <summary>
        /// Processes input from the connection (sent by the server) in a way that is specific to the terminal implementation.
        /// </summary>
        /// <param name="ch">The received character.</param>
        protected override void ProcessConnectionInput(char ch)
        {
            using (var modifier = this.Screen.GetModifier())
            {
                switch (ch)
                {
                    case '\r':
                        modifier.CursorColumn = 0;
                        break;
                    case '\n':
                        modifier.CursorRowIncreaseWithScroll(scrollTop: null, scrollBottom: null);
                        break;
                    case '\t':
                        var previousTabStop = ((modifier.CursorColumn / DefaultTabStopWidth) * DefaultTabStopWidth);
                        var nextTabStop = previousTabStop + DefaultTabStopWidth;
                        modifier.CursorColumn = Math.Min(nextTabStop, this.Screen.ColumnCount - 1);
                        break;
                    default:
                        modifier.CursorCharacter = ch;
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
        }

        /// <summary>
        /// Processes user input in a way that is specific to the terminal implementation.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <remarks>
        /// This method receives all input that represents "characters".
        /// It does not receive: Return, Cursor keys (Up, Down, Left, Right), Tabulator, Function keys (F1 - F12), Alt/Ctrl key combinations
        /// </remarks>
        protected override void ProcessUserInput(string str)
        {
            this.Transmit(str);
        }

        /// <summary>
        /// Processes user input in a way that is specific to the terminal implementation.
        /// </summary>
        /// <param name="key">The input key.</param>
        /// <param name="keyModifiers">The key modifiers.</param>
        /// <returns>A value indicating whether the key press was processed by the terminal implementation.</returns>
        /// <remarks>
        /// This method receives key presses of non-character keys (e.g. Up, Down, Left, Right, Function keys F1-F12, Alt/Ctrl key combinations, ...).
        /// </remarks>
        protected override bool ProcessUserInput(VirtualKey key, KeyModifiers keyModifiers)
        {
            string str = string.Empty;
            switch (key)
            {
                case VirtualKey.Enter:
                    str = Environment.NewLine;
                    break;
                default:
                    return false;
            }

            this.Transmit(str);

            return true;
        }
    }
}
