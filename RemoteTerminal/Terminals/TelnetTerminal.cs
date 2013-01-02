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
    public class TelnetTerminal : AbstractTerminal, ITerminal, IConnectionInitializingTerminal
    {
        private const int DefaultTabStopWidth = 8;

        public TelnetTerminal(ConnectionData connectionData)
            : base(connectionData, localEcho: true, writtenNewLine: "\r\n")
        {
        }

        public override string TerminalName
        {
            get { return "telnet"; }
        }

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

        protected override void ProcessUserInput(char ch)
        {
            // This method receives all input that represents "characters".
            // It does not receive: Return, Cursor keys (Up, Down, Left, Right), Tabulator, Function keys (F1 - F12), 
            this.Transmit(ch.ToString());
        }

        /// <summary>
        /// Processes key presses of non-character keys (e.g. Up, Down, Left, Right, Function keys F1-F12, ...).
        /// </summary>
        /// <param name="key">The pressed key.</param>
        /// <returns>true if the terminal handled the key press; false otherwise.</returns>
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
