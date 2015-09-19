using System;
using RemoteTerminal.Model;
using RemoteTerminal.Screens;
using Windows.System;

namespace RemoteTerminal.Terminals
{
    public class SshTerminal : AbstractTerminal, ITerminal, IConnectionInitializingTerminal
    {
        private class SavedCursor
        {
            public SavedCursor(int cursorRow, int cursorColumn, ScreenCellFormat currentFormat)
            {
                this.CursorRow = cursorRow;
                this.CursorColumn = cursorColumn;
                this.Format = currentFormat.Clone();
            }

            public int CursorRow { get; private set; }
            public int CursorColumn { get; private set; }
            public ScreenCellFormat Format { get; private set; }
        }

        // Escape sequence processing
        private string escapeChars = null;
        private string escapeArgs = null;
        private bool escapeCharsWaitForStringTerminator = false;

        // Fields used by escape sequences
        private int? scrollTop = null;
        private int? scrollBottom = null;
        private SavedCursor savedCursor = null;
        private bool applicationCursorKeys = false;
        private bool insertMode = false;

        private bool autoWrapMode = true;
        private bool wrapNextChar = false;

        private bool ignoreStringUserInput = false;

        private ScreenCellFormat currentFormat = new ScreenCellFormat();

        //public static double TerminalCellFontSize { get { return 17d; } }
        //public static double TerminalCellWidth { get { return 9d; } }
        //public static double TerminalCellHeight { get { return 20d; } }

        /// <summary>
        /// The default tab stop width.
        /// </summary>
        private const int DefaultTabStopWidth = 8;

        public SshTerminal(ConnectionData connectionData, bool localEcho)
            : base(connectionData, localEcho, writtenNewLine: "\r")
        {
        }

        public override string TerminalName
        {
            get { return "xterm"; }
        }

        protected override void ProcessConnectionInput(char ch)
        {
            if (this.escapeChars != null)
            {
                this.ProcessEscapeSequenceChar(ch);
            }
            else
            {
                using (var modifier = this.Screen.GetModifier())
                {
                    switch (ch)
                    {
                        case '\0':
                            break;
                        case '\x0f': // shift in ??
                        case '\x0e': // shift out ??
                            break;
                        case '\u001b':
                            this.escapeChars = string.Empty;
                            this.escapeArgs = string.Empty;
                            // As soon as an ANSI escape sequence is received we turn off the local echo 
                            // that may be enabled because of a telnet connection.
                            this.LocalEcho = false;
                            break;
                        case '\r':
                            modifier.CursorColumn = 0;
                            this.wrapNextChar = false;
                            break;
                        case '\n':
                            modifier.CursorRowIncreaseWithScroll(this.scrollTop, this.scrollBottom);
                            this.wrapNextChar = false;
                            break;
                        case '\a':
                            break;
                        case '\t':
                            this.JumpToNextHorizontalTabStop(modifier);
                            break;
                        case '\b':
                            if (modifier.CursorColumn - 1 >= 0)
                            {
                                modifier.CursorColumn--;
                            }

                            this.wrapNextChar = false;

                            break;
                        default:
                            int width = CjkWidth.CharWidth(ch);

                            if (this.autoWrapMode && this.wrapNextChar)
                            {
                                modifier.CursorColumn = 0;
                                this.wrapNextChar = false;
                                modifier.CursorRowIncreaseWithScroll(this.scrollTop, this.scrollBottom);
                            }

                            if (this.insertMode)
                            {
                                modifier.InsertCells(width);
                            }

                            modifier.CursorCharacter = ch;
                            modifier.ApplyFormatToCursor(this.currentFormat);

                            if (width > 1)
                            {
                                modifier.CursorColumn++;
                                modifier.CursorCharacter = CjkWidth.UCSWIDE;
                                modifier.ApplyFormatToCursor(this.currentFormat);
                            }

                            if (modifier.CursorColumn + 1 >= this.Screen.ColumnCount)
                            {
                                this.wrapNextChar = true;
                            }
                            else
                            {
                                modifier.CursorColumn++;
                            }

                            break;
                    }
                }
            }
        }

        protected override void ProcessUserInput(string str)
        {
            // This method receives all input that represents "characters".
            // It does not receive: Return, Cursor keys (Up, Down, Left, Right), Tabulator, Function keys (F1 - F12), Alt/Ctrl key combinations

            // This field is set to true if the other ProcessUserInput method has already processed something that also reaches this method.
            if (this.ignoreStringUserInput)
            {
                this.ignoreStringUserInput = false;
                return;
            }

            this.Transmit(str);
        }

        /// <summary>
        /// Processes key presses of non-character keys (e.g. Up, Down, Left, Right, Function keys F1-F12, Alt/Ctrl key combinations, ...).
        /// </summary>
        /// <param name="key">The pressed key.</param>
        protected override bool ProcessUserInput(VirtualKey key, KeyModifiers keyModifiers)
        {
            string input;

            // handle Alt key combinations
            if (keyModifiers.HasFlag(KeyModifiers.Alt))
            {
                if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
                {
                    input = "\x1b" + (char)key;
                }
                else if (key >= VirtualKey.A && key <= VirtualKey.Z)
                {
                    // the "+ 32" produces lower case characters
                    input = "\x1b" + (char)(key + (keyModifiers.HasFlag(KeyModifiers.Shift) ? 0 : 32));
                }
                else if (key == VirtualKey.Shift || key == VirtualKey.Menu || key == VirtualKey.Control)
                {
                    return false;
                }
                else
                {
                    return false;
                }
            }
            else if (keyModifiers == KeyModifiers.Ctrl)
            {
                if (key == VirtualKey.Space)
                {
                    this.ignoreStringUserInput = true;
                    input = "\0";
                }
                else if (key == VirtualKey.Shift || key == VirtualKey.Menu || key == VirtualKey.Control)
                {
                    return false;
                }
                else if (key == (VirtualKey)191) // Ctrl + / (US keyboard)
                {
                    input = "\x1f";
                }
                else
                {
                    return false;
                }
            }
            else
            {
                switch (key)
                {
                    case VirtualKey.Back:
                        this.ignoreStringUserInput = true;
                        input = "\x7f";
                        break;
                    case VirtualKey.Enter:
                        input = Environment.NewLine;
                        break;
                    case VirtualKey.Tab:
                        input = "\t";
                        break;
                    case VirtualKey.Left:
                        input = this.applicationCursorKeys ? "\u001bOD" : "\u001b[D";
                        break;
                    case VirtualKey.Up:
                        input = this.applicationCursorKeys ? "\u001bOA" : "\u001b[A";
                        break;
                    case VirtualKey.Right:
                        input = this.applicationCursorKeys ? "\u001bOC" : "\u001b[C";
                        break;
                    case VirtualKey.Down:
                        input = this.applicationCursorKeys ? "\u001bOB" : "\u001b[B";
                        break;
                    case VirtualKey.Insert:
                        input = "\u001b[2~";
                        break;
                    case VirtualKey.Delete:
                        input = "\u001b[3~";
                        break;
                    case VirtualKey.Home:
                        input = "\u001b[1~";
                        break;
                    case VirtualKey.End:
                        input = "\u001b[4~";
                        break;
                    case VirtualKey.PageUp:
                        input = "\u001b[5~";
                        break;
                    case VirtualKey.PageDown:
                        input = "\u001b[6~";
                        break;
                    case VirtualKey.F1:
                        input = "\u001b[11" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.F2:
                        input = "\u001b[12" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.F3:
                        input = "\u001b[13" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.F4:
                        input = "\u001b[14" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.F5:
                        input = "\u001b[15" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.F6:
                        input = "\u001b[17" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.F7:
                        input = "\u001b[18" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.F8:
                        input = "\u001b[19" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.F9:
                        input = "\u001b[20" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.F10:
                        input = "\u001b[21" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.F11:
                        input = "\u001b[23" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.F12:
                        input = "\u001b[24" + GetFunctionKeyModifier(keyModifiers) + "~";
                        break;
                    case VirtualKey.Shift:
                    case VirtualKey.Menu:
                    case VirtualKey.Control:
                        return false;
                    default:
                        return false;
                }
            }

            this.Transmit(input);

            return true;
        }

        private static string GetFunctionKeyModifier(KeyModifiers keyModifiers)
        {
            return string.Empty;

            bool isShiftKeyDown = keyModifiers.HasFlag(KeyModifiers.Shift);
            bool isAltKeyDown = keyModifiers.HasFlag(KeyModifiers.Alt);
            bool isControlKeyDown = keyModifiers.HasFlag(KeyModifiers.Ctrl);

            if (isShiftKeyDown && !isAltKeyDown && !isControlKeyDown)
            {
                return ";2";
            }
            else if (!isShiftKeyDown && isAltKeyDown && !isControlKeyDown)
            {
                return ";3";
            }
            else if (isShiftKeyDown && isAltKeyDown && !isControlKeyDown)
            {
                return ";4";
            }
            else if (!isShiftKeyDown && !isAltKeyDown && isControlKeyDown)
            {
                return ";5";
            }
            else if (isShiftKeyDown && !isAltKeyDown && isControlKeyDown)
            {
                return ";6";
            }
            else if (!isShiftKeyDown && isAltKeyDown && isControlKeyDown)
            {
                return ";7";
            }
            else if (isShiftKeyDown && isAltKeyDown && isControlKeyDown)
            {
                return ";8";
            }

            return string.Empty;
        }

        private void ProcessEscapeSequenceChar(char ch)
        {
            // From http://en.wikipedia.org/wiki/ANSI_escape_code#Sequence_elements:
            //
            // Escape sequences start with the character ESC (ASCII decimal 27/hex 0x1B/octal 033). For two character
            // sequences, the second character is in the range ASCII 64 to 95 (@ to _). However, most of the sequences
            // are more than two characters, and start with the characters ESC and [ (left bracket). This sequence is
            // called CSI for Control Sequence Introducer (or Control Sequence Initiator). The final character of these
            // sequences is in the range ASCII 64 to 126 (@ to ~).
            // 
            // There is a single-character CSI (155/0x9B/0233) as well. The ESC+[ two-character sequence is more often
            // used than the single-character alternative, for details see C0 and C1 control codes. Only the
            // two-character sequence is recognized by devices that support just ASCII (7-bit bytes) or devices that
            // support 8-bit bytes but use the 0x80–0x9F control character range for other purposes. On terminals that
            // use UTF-8 encoding, both forms take 2 bytes (CSI in UTF-8 is 0xC2, 0x9B)[discuss] but the ESC+[ sequence
            // is clearer.

            // Parser description:
            // http://www.vt100.net/emu/dec_ansi_parser

            // TODO: consider the above description...

            //if (this.escapeChars.Length == 0 && ch >= 64 && ch <= 95)
            //{
            //    this.escapeChars += ch;
            //}

            if (this.escapeCharsWaitForStringTerminator)
            {
                if (ch == '\a')
                {
                    this.escapeChars += ch;
                }
                else if (ch == '\u001b')
                {
                    this.escapeChars += ch;
                    return;
                }
                else if (ch == '\\' && this.escapeChars[this.escapeChars.Length - 1] == '\u001b')
                {
                    this.escapeChars += ch;
                }
                else
                {
                    if (this.escapeChars[this.escapeChars.Length - 1] == '\u001b')
                    {
                        this.escapeArgs += this.escapeChars[this.escapeChars.Length - 1];
                        this.escapeChars = this.escapeChars.Remove(this.escapeChars.Length - 1);
                    }

                    this.escapeArgs += ch.ToString();
                    return;
                }
            }
            else if (this.escapeChars.Length == 0 && "78".IndexOf(ch) >= 0)
            {
                this.escapeChars += ch.ToString();
            }
            else if (this.escapeChars.Length > 0 && "()Y".IndexOf(this.escapeChars[0]) >= 0)
            {
                this.escapeChars += ch.ToString();
                if (this.escapeChars.Length != (this.escapeChars[0] == 'Y' ? 3 : 2)) return;
            }
            else if (this.escapeChars.Length == 1 && this.escapeChars[0] == '#')
            {
                this.escapeChars += ch.ToString();
            }
            else if (ch == ';' || char.IsDigit(ch))
            {
                this.escapeArgs += ch.ToString();
                return;
            }
            else
            {
                if ("[#?()Y".IndexOf(ch) >= 0)
                {
                    this.escapeChars += ch.ToString();
                    return;
                }
                else if (this.escapeChars.Length == 0 && ch == ']')
                {
                    this.escapeCharsWaitForStringTerminator = true;
                    this.escapeChars += ch.ToString();
                    return;
                }

                this.escapeChars += ch.ToString();
            }

            this.ProcessEscapeSequence();

            this.escapeChars = null;
            this.escapeArgs = null;
            this.escapeCharsWaitForStringTerminator = false;
        }

        private void ProcessEscapeSequence()
        {
            //if (this.escapeChars.StartsWith("Y"))
            //{
            //    Row = (int)this.escapeChars[1] - 64;
            //    Column = (int)this.escapeChars[2] - 64;
            //    return;
            //}
            //if (this.vt52Mode && (this.escapeChars == "D" || this.escapeChars == "H")) this.escapeChars += "_";

            var chars = this.escapeChars;
            var args = this.escapeArgs.Split(';');

            int? arg0 = null;
            if (args.Length > 0 && args[0] != "")
            {
                int arg;
                if (int.TryParse(args[0], out arg))
                {
                    arg0 = arg;
                }
            }

            int? arg1 = null;
            if (args.Length > 1 && args[1] != "")
            {
                int arg;
                if (int.TryParse(args[1], out arg))
                {
                    arg1 = arg;
                }
            }

            using (var modifier = this.Screen.GetModifier())
            {
                int cursorRow = modifier.CursorRow;
                int cursorColumn = modifier.CursorColumn;

                switch (chars)
                {
                    // Device Status
                    case "[c":
                        if (this.IsConnected)
                        {
                            this.Transmit("\u001b[?1;0c");
                        }

                        break;
                    case "[A":
                        //case "A":
                        cursorRow -= Math.Max(arg0 ?? 1, 1);
                        this.wrapNextChar = false;
                        break;
                    case "[B":
                        //case "B":
                        cursorRow += Math.Max(arg0 ?? 1, 1);
                        this.wrapNextChar = false;
                        break;
                    case "[C":
                        //case "C":
                        cursorColumn += Math.Max(arg0 ?? 1, 1);
                        this.wrapNextChar = false;
                        break;
                    case "[D":
                        //case "D":
                        cursorColumn -= Math.Max(arg0 ?? 1, 1);
                        this.wrapNextChar = false;
                        break;

                    case "[d":
                        cursorRow = Math.Max(arg0 ?? 1, 1) - 1;
                        this.wrapNextChar = false;
                        break;
                    case "[G":
                        cursorColumn = Math.Max(arg0 ?? 1, 1) - 1;
                        this.wrapNextChar = false;
                        break;
                    case "[f":
                    case "[H":
                        //case "H_":
                        cursorRow = Math.Max(arg0 ?? 1, 1) - 1;
                        cursorColumn = Math.Max(arg1 ?? 1, 1) - 1;
                        this.wrapNextChar = false;
                        break;

                    case "M":
                        modifier.CursorRowDecreaseWithScroll(this.scrollTop, this.scrollBottom);
                        this.wrapNextChar = false;
                        break;
                    case "D":
                        modifier.CursorRowIncreaseWithScroll(this.scrollTop, this.scrollBottom);
                        this.wrapNextChar = false;
                        break;
                    case "E":
                        modifier.CursorColumn = 0;
                        modifier.CursorRowIncreaseWithScroll(this.scrollTop, this.scrollBottom);
                        this.wrapNextChar = false;
                        break;
                    case "[S":
                        modifier.ScrollUp(arg0 ?? 1, this.scrollTop, this.scrollBottom);
                        this.wrapNextChar = false;
                        break;
                    case "[T":
                        modifier.ScrollDown(arg0 ?? 1, this.scrollTop, this.scrollBottom);
                        this.wrapNextChar = false;
                        break;

                    case "[L":
                        modifier.InsertLines(arg0 ?? 1, this.scrollTop, this.scrollBottom);
                        break;
                    case "[M":
                        modifier.DeleteLines(arg0 ?? 1, this.scrollTop, this.scrollBottom);
                        break;

                    case "[@":
                        modifier.InsertCells(arg0 ?? 1);
                        break;
                    case "[P":
                        modifier.DeleteCells(arg0 ?? 1);
                        break;

                    case "[r":
                        this.scrollTop = (arg0 ?? 1) - 1;
                        this.scrollBottom = (arg1 ?? this.Rows) - 1;
                        break;

                    //case "H": if (!_tabStops.Contains(Column)) _tabStops.Add(Column); break;
                    //case "g": if (arg0 == 3) _tabStops.Clear(); else _tabStops.Remove(Column); break;

                    case "[J":
                        //case "J":
                        switch (arg0 ?? 0)
                        {
                            case 0: modifier.Erase(cursorRow, cursorColumn, this.Rows - 1, this.Columns - 1, this.currentFormat); break;
                            case 1: modifier.Erase(0, 0, cursorRow, cursorColumn, this.currentFormat); break;
                            case 2: modifier.Erase(0, 0, this.Rows - 1, this.Columns - 1, this.currentFormat); break;
                        }
                        break;
                    case "[K":
                        //case "K":
                        switch (arg0 ?? 0)
                        {
                            case 0: modifier.Erase(cursorRow, cursorColumn, cursorRow, this.Columns - 1, this.currentFormat); break;
                            case 1: modifier.Erase(cursorRow, 0, cursorRow, cursorColumn, this.currentFormat); break;
                            case 2: modifier.Erase(cursorRow, 0, cursorRow, this.Columns - 1, this.currentFormat); break;
                        }
                        break;
                    case "[X":
                        int endRow = cursorRow;
                        int endColumn = cursorColumn + ((arg0 ?? 1) - 1);
                        while (endColumn > this.Columns)
                        {
                            endRow++;
                            endColumn -= this.Columns;
                        }

                        modifier.Erase(cursorRow, cursorColumn, endRow, endColumn, this.currentFormat);
                        break;

                    case "[l":
                    case "[h":
                        var enableMode = chars == "[h";
                        switch (arg0)
                        {
                            case 4:
                                this.insertMode = enableMode;
                                break;
                            default:
                                break;
                        }
                        break;

                    case "[?l":
                    case "[?h":
                        var h = chars == "[?h";
                        switch (arg0)
                        {
                            case 1:
                                this.applicationCursorKeys = h;
                                break;
                            //        case 2: _vt52Mode = h; break;
                            //        case 3: Width = h ? 132 : 80; ResetBuffer(); break;
                            case 7:
                                this.autoWrapMode = h;
                                break;
                            case 12:
                                // TODO: aptitude (Stop Blinking Cursor)
                                break;
                            case 25:
                                // TODO: aptitude (Hidden Cursor)
                                // this.Display.CursorHidden = h;
                                break;
                            case 47:
                                modifier.SwitchBuffer(alternateBuffer: h);
                                break;
                            case 1000:
                                // TODO: aptitude (Don’t Send Mouse X & Y on button press and release)
                                break;
                            case 1002:
                                // TODO: mc (Use Cell Motion Mouse Tracking)
                                break;
                            case 1047: // Use Alternate Screen Buffer
                                this.scrollTop = null;
                                this.scrollBottom = null;
                                modifier.SwitchBuffer(alternateBuffer: h);
                                if (!h)
                                {
                                    modifier.Erase(0, 0, this.Rows - 1, this.Columns - 1, this.currentFormat);
                                }
                                break;
                            case 1048:
                                this.SaveOrRestoreCursor(ref cursorRow, ref cursorColumn, save: h);
                                break;
                            case 1049:
                                this.SaveOrRestoreCursor(ref cursorRow, ref cursorColumn, save: h);
                                this.scrollTop = null;
                                this.scrollBottom = null;
                                modifier.SwitchBuffer(alternateBuffer: h);
                                if (h)
                                {
                                    modifier.Erase(0, 0, this.Rows - 1, this.Columns - 1, this.currentFormat);
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    //case "<": _vt52Mode = false; break;

                    case "[m":
                        //case "m":
                        if (this.escapeArgs.Length == 0)
                        {
                            this.currentFormat.Reset();
                            break;
                        }
                        // 256-color support:
                        else if (args.Length == 3 && (arg0 == 38 || arg0 == 48) && arg1 == 5)
                        {
                            int arg2;
                            if (args[2] == "" || !int.TryParse(args[2], out arg2))
                            {
                                break;
                            }

                            byte colorIndex = (byte)arg2;
                            if (arg0 == 38)
                            {
                                this.currentFormat.ForegroundColor = (ScreenColor)colorIndex;
                            }
                            else if (arg0 == 48)
                            {
                                this.currentFormat.BackgroundColor = (ScreenColor)colorIndex;
                            }

                            break;
                        }

                        foreach (var arg in args)
                        {
                            switch (arg)
                            {
                                case "0":
                                case "00":
                                    this.currentFormat.Reset();
                                    break;
                                case "1":
                                case "01":
                                    this.currentFormat.BoldMode = true;
                                    break;
                                //            case "2": _lowMode = true; break;
                                case "4":
                                    this.currentFormat.UnderlineMode = true;
                                    break;
                                //            case "5": _blinkMode = true; break;
                                case "7":
                                    this.currentFormat.ReverseMode = true;
                                    break;
                                //            case "8": _invisibleMode = true; break;
                                case "22":
                                    this.currentFormat.BoldMode = false;
                                    break;
                                case "24":
                                    this.currentFormat.UnderlineMode = false;
                                    break;
                                case "27":
                                    this.currentFormat.ReverseMode = false;
                                    break;
                                case "30": this.currentFormat.ForegroundColor = ScreenColor.Black; break;
                                case "31": this.currentFormat.ForegroundColor = ScreenColor.Red; break;
                                case "32": this.currentFormat.ForegroundColor = ScreenColor.Green; break;
                                case "33": this.currentFormat.ForegroundColor = ScreenColor.Yellow; break;
                                case "34": this.currentFormat.ForegroundColor = ScreenColor.Blue; break;
                                case "35": this.currentFormat.ForegroundColor = ScreenColor.Magenta; break;
                                case "36": this.currentFormat.ForegroundColor = ScreenColor.Cyan; break;
                                case "37": this.currentFormat.ForegroundColor = ScreenColor.White; break;
                                case "39": this.currentFormat.ForegroundColor = ScreenColor.DefaultForeground; break;
                                case "40": this.currentFormat.BackgroundColor = ScreenColor.Black; break;
                                case "41": this.currentFormat.BackgroundColor = ScreenColor.Red; break;
                                case "42": this.currentFormat.BackgroundColor = ScreenColor.Green; break;
                                case "43": this.currentFormat.BackgroundColor = ScreenColor.Yellow; break;
                                case "44": this.currentFormat.BackgroundColor = ScreenColor.Blue; break;
                                case "45": this.currentFormat.BackgroundColor = ScreenColor.Magenta; break;
                                case "46": this.currentFormat.BackgroundColor = ScreenColor.Cyan; break;
                                case "47": this.currentFormat.BackgroundColor = ScreenColor.White; break;
                                case "49": this.currentFormat.BackgroundColor = ScreenColor.DefaultBackground; break;
                                default:
                                    break;
                            }
                        }
                        //    UpdateBrushes();
                        break;
                    case "]\a":
                    case "]\u001b\\":
                        switch (arg0)
                        {
                            case 0:
                            case 1:
                            case 2:
                                this.Title = args[1];
                                break;
                            case 3:
                                break;
                            case 4:
                                // Change the 256 color palette
                                ////if (arg1 == null || args.Length < 3)
                                ////{
                                ////    break;
                                ////}

                                ////byte colorIndex = (byte)arg1;

                                ////// colors below index 16 may not be modified
                                ////if (colorIndex < 16)
                                ////{
                                ////    break;
                                ////}

                                ////Color? color = XParseColor(args[2]);
                                ////if (color != null)
                                ////{
                                ////    this.paletteColors[colorIndex] = color.Value;
                                ////}

                                break;
                        }
                        break;
                    //case "#3":
                    //case "#4":
                    //case "#5":
                    //case "#6":
                    //    _doubleMode = (CharacterDoubling)((int)_escapeChars[1] - (int)'0');
                    //    break;

                    //case "[s": _saveRow = Row; _saveColumn = Column; break;
                    case "7":
                        this.SaveOrRestoreCursor(ref cursorRow, ref cursorColumn, save: true);
                        break;
                    //case "[u": Row = _saveRow; Column = _saveColumn; break;
                    case "8":
                        this.SaveOrRestoreCursor(ref cursorRow, ref cursorColumn, save: false);
                        break;

                    //case "c": Reset(); break;

                    case "(0":
                        // TODO: aptitude ??
                        break;
                    case ")0":
                        // TODO: nano auf shellmix.com ??
                        break;
                    case "(B":
                        // TODO: nano ??
                        break;
                    case "=":
                        // TODO: nano (Application Keypad)
                        break;
                    case ">":
                        // TODO: nano (Normal Keypad)
                        break;
                    case "[?s":
                        // TODO: mc (Save DEC Private Mode Values)
                        break;
                    case "[?r":
                        // TODO: mc (Restore DEC Private Mode Values)
                        break;

                    // TODO: Character set selection, several esoteric ?h/?l modes
                    default:
                        break;
                }

                modifier.CursorRow = Math.Min(Math.Max(cursorRow, 0), this.Rows - 1);
                modifier.CursorColumn = Math.Min(Math.Max(cursorColumn, 0), this.Columns - 1);
            }
        }

        private void SaveOrRestoreCursor(ref int cursorRow, ref int cursorColumn, bool save)
        {
            if (save)
            {
                this.savedCursor = new SavedCursor(cursorRow, cursorColumn, this.currentFormat);
            }
            else
            {
                if (this.savedCursor == null)
                {
                    return;
                }

                cursorRow = this.savedCursor.CursorRow;
                cursorColumn = this.savedCursor.CursorColumn;
                this.currentFormat = this.savedCursor.Format.Clone();
            }
        }

        ////private static Color? XParseColor(string colorString)
        ////{
        ////    if (colorString.StartsWith("rgb:", StringComparison.Ordinal) && colorString.Length > 4)
        ////    {
        ////        string[] rgbStrings = colorString.Substring(4).Split('/');
        ////        if (rgbStrings.Length != 3)
        ////        {
        ////            return null;
        ////        }

        ////        byte[] rgbValues = new byte[3];
        ////        for (int i = 0; i < 3; i++)
        ////        {
        ////            if (rgbStrings[i].Length == 0 || rgbStrings.Length > 4)
        ////            {
        ////                return null;
        ////            }

        ////            int value;
        ////            try
        ////            {
        ////                value = Convert.ToInt32(rgbStrings[i], 16);
        ////                double scale = 256d / Math.Pow(2d, rgbStrings[i].Length * 4d);
        ////                rgbValues[i] = (byte)(value * scale);
        ////            }
        ////            catch (Exception)
        ////            {
        ////                return null;
        ////            }
        ////        }

        ////        return Color.FromArgb(255, rgbValues[0], rgbValues[1], rgbValues[2]);
        ////    }

        ////    return null;
        ////}

        private void JumpToNextHorizontalTabStop(IScreenModifier modifier)
        {
            var previousTabStop = ((modifier.CursorColumn / DefaultTabStopWidth) * DefaultTabStopWidth);
            var nextTabStop = previousTabStop + DefaultTabStopWidth;
            modifier.CursorColumn = Math.Min(nextTabStop, this.Columns - 1);
        }
    }
}
