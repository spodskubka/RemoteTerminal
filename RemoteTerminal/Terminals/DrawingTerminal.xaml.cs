using System;
using System.Threading;
using System.Threading.Tasks;
using CommonDX;
using RemoteTerminal.Connections;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Automation.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace RemoteTerminal.Terminals
{
    public sealed partial class DrawingTerminal : UserControl, ITerminal
    {
        private class SavedCursor
        {
            public SavedCursor(int cursorRow, int cursorColumn, DrawingTerminalCellFormat currentFormat)
            {
                this.CursorRow = cursorRow;
                this.CursorColumn = cursorColumn;
                this.Format = currentFormat.Clone();
            }

            public int CursorRow { get; private set; }
            public int CursorColumn { get; private set; }
            public DrawingTerminalCellFormat Format { get; private set; }
        }

        private IConnection connection = null;
        private Task readerTask;

        private readonly DrawingTerminalDisplay Display = new DrawingTerminalDisplay();

        // DirectX stuff
        private readonly DeviceManager deviceManager;
        private DrawingTerminalRenderer terminalRenderer = null;
        private SurfaceImageSourceTarget d2dTarget = null;

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

        private DrawingTerminalCellFormat currentFormat = new DrawingTerminalCellFormat();

        public bool LocalEcho { get; set; }
        public string WrittenNewLine { get; set; }

        public static double TerminalCellFontSize { get { return 17d; } }
        public static double TerminalCellWidth { get { return 9d; } }
        public static double TerminalCellHeight { get { return 20d; } }

        /// <summary>
        /// The default tab stop width.
        /// </summary>
        private const int DefaultTabStopWidth = 8;

        private string localReadPrompt = string.Empty;
        private AutoResetEvent localReadSync = null;
        private string localReadLine = string.Empty;
        private int localReadStartColumn = 0;
        private int localReadStartRow = 0;

        public DrawingTerminal()
        {
            this.InitializeComponent();
            this.IsTabStop = true;
            this.IsTapEnabled = true;

            this.terminalFrame.BorderBrush = new SolidColorBrush(Colors.Gray);
            this.terminalFrame.BorderThickness = new Thickness(2d);

            this.LocalEcho = true;
            this.WrittenNewLine = "\r\n";

            this.Columns = 80;
            this.Rows = 24;

            this.UpdateCursor();

            this.deviceManager = new DeviceManager();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double terminalRectangleWidth = finalSize.Width - this.terminalFrame.BorderThickness.Left - this.terminalFrame.BorderThickness.Right;
            double terminalRectangleHeight = finalSize.Height - this.terminalFrame.BorderThickness.Top - this.terminalFrame.BorderThickness.Bottom;
            int pixelWidth = (int)(terminalRectangleWidth * DisplayProperties.LogicalDpi / 96.0);
            int pixelHeight = (int)(terminalRectangleHeight * DisplayProperties.LogicalDpi / 96.0);

            lock (this.Display.ChangeLock)
            {
                this.DetachRenderer();
                this.AttachRenderer(pixelWidth, pixelHeight);

                this.Display.Changed = true;
            }

            this.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                int rows = (int)((finalSize.Height - 4) / DrawingTerminal.TerminalCellHeight);
                int columns = (int)((finalSize.Width - 4) / DrawingTerminal.TerminalCellWidth);

                if (this.Rows != rows || this.Columns != columns)
                {
                    if (localReadSync != null)
                    {
                        this.Erase(localReadStartRow, localReadStartColumn, this.Display.CursorRow, this.Display.CursorColumn);
                        this.Display.CursorColumn = this.localReadStartColumn;
                        this.Display.CursorRow = this.localReadStartRow;
                        this.UpdateCursor();
                    }

                    this.UpdateDisplay(rows, columns);

                    if (this.IsConnected)
                    {
                        this.connection.ResizeTerminal(columns, rows);
                    }

                    if (this.localReadSync != null)
                    {
                        this.ProcessInput(this.localReadPrompt);
                        this.ProcessInput(this.LocalEcho ? this.localReadLine : new string('●', this.localReadLine.Length));
                    }
                }
            });

            return base.ArrangeOverride(finalSize);
        }

        void CompositionTarget_Rendering(object sender, object e)
        {
            lock (this.Display.ChangeLock)
            {
                if (!this.Display.Changed)
                {
                    return;
                }
            }

            lock (this.deviceManager)
            {
                if (this.d2dTarget == null)
                {
                    return;
                }

                this.d2dTarget.RenderAll();
            }
        }

        private void UpdateDisplay(int rows, int columns)
        {
            lock (this.Display.ChangeLock)
            {
                while (this.Display.Lines.Count > rows)
                {
                    this.Display.Lines.RemoveAt(0);
                }

                foreach (DrawingTerminalLine line in this.Display.Lines)
                {
                    while (line.Cells.Count > columns)
                    {
                        line.Cells.RemoveAt(line.Cells.Count - 1);
                    }
                }
            }

            this.Rows = rows;
            this.Columns = columns;

            this.Display.CursorRow = Math.Min(this.Rows - 1, this.Display.CursorRow);
            this.Display.CursorColumn = Math.Min(this.Columns - 1, this.Display.CursorColumn);
        }

        private void UpdateCursor()
        {
            ////this.Display.Cursor.Visibility = this.IsConnected ? Visibility.Visible : Visibility.Collapsed;
            lock (this.Display.ChangeLock)
            {
                var cursorCell = this.CursorCell;
                this.Display.Changed = true;
            }
        }

        /// <summary>
        /// Create the Automation peer implementations for DrawingTerminal to provide the accessibility support.
        /// </summary>
        /// <returns>Automation Peer implementation for this control</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DrawingTerminalAutomationPeer(this);
        }

        /// <summary>
        /// Override the default event handler for GotFocus.
        /// When the control got focus, indicate it has focus by highlighting the control by changing the background color to yellow.
        /// </summary>
        /// <param name="e">State information and event data associated with GotFocus event.</param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            //this.frame.BorderBrush = new SolidColorBrush(this.IsConnected ? Colors.Yellow : Colors.Red);
            this.Display.HasFocus = true;
            this.UpdateCursor();
            CoreWindow.GetForCurrentThread().CharacterReceived += Terminal_CharacterReceived;
        }

        /// <summary>
        /// Override the default event handler for LostFocus.
        /// When the control lost focus, indicate it does not have focus by changing the background color to gray.
        /// And the content is cleared.
        /// </summary>
        /// <param name="e">State information and event data associated with LostFocus event.</param>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            //this.frame.BorderBrush = new SolidColorBrush(this.IsConnected ? Colors.Gray : Colors.Red);
            this.Display.HasFocus = false;
            this.UpdateCursor();
            CoreWindow.GetForCurrentThread().CharacterReceived -= Terminal_CharacterReceived;
        }

        /// <summary>
        /// Override the default event handler for Tapped.
        /// Set input focus to the control when tapped on.
        /// </summary>
        /// <param name="e">State information and event data associated with Tapped event.</param>
        protected override void OnTapped(TappedRoutedEventArgs e)
        {
            this.Focus(FocusState.Pointer);
        }

        /// <summary>
        /// Override the default event handler for KeyDown.  
        /// Displays the text "A key is pressed" and the approximate time when the key is pressed.
        /// </summary>
        /// <param name="e">State information and event data associated with KeyDown event.</param>
        async void Terminal_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            // This method receives all input that represents "characters".
            // It does not receive: Return, Cursor keys (Up, Down, Left, Right), Tabulator, Function keys (F1 - F12), 
            string ch = ((char)args.KeyCode).ToString();

            if (this.localReadSync != null)
            {
                if (ch == "\b")
                {
                    if (this.localReadLine.Length > 0)
                    {
                        ch = "\b \b";
                        if (this.Display.CursorColumn == 0)
                        {
                            ch = "\u001b[" + this.Display.CursorRow + ";" + this.Columns + "H \u001b[" + this.Display.CursorRow + ";" + this.Columns + "H";
                        }
                        this.localReadLine = this.localReadLine.Substring(0, this.localReadLine.Length - 1);
                    }
                    else
                    {
                        ch = "\a";
                    }
                    this.ProcessInput(ch);
                }
                else
                {
                    this.localReadLine += ch;
                    if (this.LocalEcho)
                    {
                        this.ProcessInput(ch);
                    }
                    else
                    {
                        this.ProcessInput(new string('●', ch.Length));
                    }
                }
            }
            else
            {
                if (!this.IsConnected)
                {
                    return;
                }

                this.connection.Write(ch);

                if (this.LocalEcho)
                {
                    this.ProcessInput(ch);
                }
            }

            args.Handled = true;
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            string input;
            switch (e.Key)
            {
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
                    input = "\u001b[11" + GetFunctionKeyModifier() + "~";
                    break;
                case VirtualKey.F2:
                    input = "\u001b[12" + GetFunctionKeyModifier() + "~";
                    break;
                case VirtualKey.F3:
                    input = "\u001b[13" + GetFunctionKeyModifier() + "~";
                    break;
                case VirtualKey.F4:
                    input = "\u001b[14" + GetFunctionKeyModifier() + "~";
                    break;
                case VirtualKey.F5:
                    input = "\u001b[15" + GetFunctionKeyModifier() + "~";
                    break;
                case VirtualKey.F6:
                    input = "\u001b[17" + GetFunctionKeyModifier() + "~";
                    break;
                case VirtualKey.F7:
                    input = "\u001b[18" + GetFunctionKeyModifier() + "~";
                    break;
                case VirtualKey.F8:
                    input = "\u001b[19" + GetFunctionKeyModifier() + "~";
                    break;
                case VirtualKey.F9:
                    input = "\u001b[20" + GetFunctionKeyModifier() + "~";
                    break;
                case VirtualKey.F10:
                    input = "\u001b[21" + GetFunctionKeyModifier() + "~";
                    break;
                case VirtualKey.F11:
                    input = "\u001b[23" + GetFunctionKeyModifier() + "~";
                    break;
                case VirtualKey.F12:
                    input = "\u001b[24" + GetFunctionKeyModifier() + "~";
                    break;
                default:
                    return;
            }

            if (this.localReadSync != null)
            {
                if (input == Environment.NewLine)
                {
                    this.ProcessInput("\r\n");
                    this.localReadSync.Set();
                }
            }
            else
            {
                if (this.LocalEcho)
                {
                    this.ProcessInput(input == Environment.NewLine ? "\r\n" : input);
                }

                if (!this.IsConnected)
                {
                    return;
                }

                this.connection.Write(input == Environment.NewLine ? this.WrittenNewLine : input);
            }

            e.Handled = true;
        }

        private static string GetFunctionKeyModifier()
        {
            return string.Empty;

            var coreWindow = Window.Current.CoreWindow;
            bool isShiftKeyDown = (coreWindow.GetKeyState(VirtualKey.Shift) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            bool isAltKeyDown = (coreWindow.GetKeyState(VirtualKey.Menu) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
            bool isControlKeyDown = (coreWindow.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

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

        public string TerminalName
        {
            get { return "xterm"; }
        }

        public bool IsConnected
        {
            get { return this.connection != null; }
        }

        public void Connect(IConnection connection)
        {
            if (this.connection != null)
            {
                throw new InvalidOperationException("Already connected.");
            }

            if (connection == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.connection = connection;
            this.readerTask = Task.Run(async () =>
            {
                try
                {
                    do
                    {
                        string content = await this.connection.ReadAsync();
                        if (content.Length <= 0)
                        {
                            break;
                        }

                        foreach (char contentChar in content)
                        {
                            this.ProcessInput(contentChar);
                        }
                    }
                    while (true);

                    this.connection = null;
                    this.Disconnect();
                }
                catch (Exception)
                {
                    throw;
                }
            });
        }

        public void Disconnect()
        {
            if (this.connection != null)
            {
                this.connection.Disconnect();
                this.connection = null;
            }

            if (this.localReadSync != null)
            {
                this.localReadLine = null;
                this.localReadSync.Set();
            }

            this.readerTask = null;
            this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.terminalFrame.BorderBrush = new SolidColorBrush(Colors.Red);
                this.IsEnabled = false;
            });
        }

        private void ProcessInput(string str)
        {
            foreach (char ch in str)
            {
                this.ProcessInput(ch);
            }
        }

        private void ProcessInput(char ch)
        {
            if (this.escapeChars != null)
            {
                this.ProcessEscapeSequenceChar(ch);
            }
            else
            {
                switch (ch)
                {
                    case '\x0f': // shift in ??
                    case '\x0e': // shift out ??
                        break;
                    case '\u001b':
                        this.escapeChars = string.Empty;
                        this.escapeArgs = string.Empty;
                        break;
                    case '\r':
                        this.Display.CursorColumn = 0;
                        break;
                    case '\n':
                        this.NextLineWithScroll();
                        break;
                    case '\a':
                        break;
                    case '\t':
                        this.JumpToNextHorizontalTabStop();
                        break;
                    case '\b':
                        if (--this.Display.CursorColumn < 0)
                        {
                            this.Display.CursorColumn = 0;
                        }

                        break;
                    default:
                        lock (this.Display.ChangeLock)
                        {
                            if (this.insertMode)
                            {
                                var line = this.Display[this.Display.CursorRow];
                                if (line.Cells.Count == this.Columns)
                                {
                                    line.Cells.RemoveAt(this.Columns - 1);
                                }

                                line.Cells.Insert(this.Display.CursorColumn, new DrawingTerminalCell(this.Display));
                            }

                            this.CursorCell.Character = ch;
                            this.CursorCell.ApplyFormat(this.currentFormat);
                            this.Display.Changed = true;
                        }

                        if (++this.Display.CursorColumn >= this.Columns)
                        {
                            if (this.autoWrapMode)
                            {
                                this.Display.CursorColumn = 0;
                                this.NextLineWithScroll();
                            }
                            else
                            {
                                --this.Display.CursorColumn;
                            }
                        }

                        break;
                }

                this.UpdateCursor();
            }
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

            switch (chars)
            {
                // Device Status
                case "[c":
                    if (this.IsConnected)
                    {
                        this.connection.Write("\u001b[?1;0c");
                    }

                    break;
                case "[A":
                    //case "A":
                    this.Display.CursorRow -= Math.Max(arg0 ?? 1, 1); break;
                case "[B":
                    //case "B":
                    this.Display.CursorRow += Math.Max(arg0 ?? 1, 1); break;
                case "[C":
                    //case "C":
                    this.Display.CursorColumn += Math.Max(arg0 ?? 1, 1); break;
                case "[D":
                    //case "D":
                    this.Display.CursorColumn -= Math.Max(arg0 ?? 1, 1); break;

                case "[d":
                    this.Display.CursorRow = Math.Max(arg0 ?? 1, 1) - 1;
                    break;
                case "[G":
                    this.Display.CursorColumn = Math.Max(arg0 ?? 1, 1) - 1;
                    break;
                case "[f":
                case "[H":
                    //case "H_":
                    this.Display.CursorRow = Math.Max(arg0 ?? 1, 1) - 1;
                    this.Display.CursorColumn = Math.Max(arg1 ?? 1, 1) - 1;
                    break;

                case "M": this.PreviousLineWithScroll(); break;
                case "D": this.NextLineWithScroll(); break;
                case "E": this.NextLineWithScroll(); this.Display.CursorColumn = 0; break;
                case "[S": this.ScrollUp(arg0 ?? 1); break;
                case "[T": this.ScrollDown(arg0 ?? 1); break;

                case "[L":
                    this.InsertLines(arg0 ?? 1);
                    break;
                case "[M":
                    this.DeleteLines(arg0 ?? 1);
                    break;

                case "[P":
                    this.DeleteCells(arg0 ?? 1);
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
                        case 0: this.Erase(this.Display.CursorRow, this.Display.CursorColumn, this.Rows - 1, this.Columns - 1); break;
                        case 1: this.Erase(0, 0, this.Display.CursorRow, this.Display.CursorColumn); break;
                        case 2: this.Erase(0, 0, this.Rows - 1, this.Columns - 1); break;
                    }
                    break;
                case "[K":
                    //case "K":
                    switch (arg0 ?? 0)
                    {
                        case 0: this.Erase(this.Display.CursorRow, this.Display.CursorColumn, this.Display.CursorRow, this.Columns - 1); break;
                        case 1: this.Erase(this.Display.CursorRow, 0, this.Display.CursorRow, this.Display.CursorColumn); break;
                        case 2: this.Erase(this.Display.CursorRow, 0, this.Display.CursorRow, this.Columns - 1); break;
                    }
                    break;
                case "[X":
                    int endRow = this.Display.CursorRow;
                    int endColumn = this.Display.CursorColumn + ((arg0 ?? 1) - 1);
                    while (endColumn > this.Columns)
                    {
                        endRow++;
                        endColumn -= this.Columns;
                    }

                    this.Erase(this.Display.CursorRow, this.Display.CursorColumn, endRow, endColumn);
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
                            // TODO: mc (Use Normal/Alternate Screen Buffer)
                            break;
                        case 1000:
                            // TODO: aptitude (Don’t Send Mouse X & Y on button press and release)
                            break;
                        case 1002:
                            // TODO: mc (Use Cell Motion Mouse Tracking)
                            break;
                        case 1049:
                            // TODO: aptitude (Use Normal Screen Buffer and restore cursor as in DECRC)
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
                            case "30": this.currentFormat.ForegroundColor = Colors.Black; break;
                            case "31": this.currentFormat.ForegroundColor = Colors.Red; break;
                            case "32": this.currentFormat.ForegroundColor = Colors.Green; break;
                            case "33": this.currentFormat.ForegroundColor = Colors.Yellow; break;
                            case "34": this.currentFormat.ForegroundColor = Colors.Blue; break;
                            case "35": this.currentFormat.ForegroundColor = Colors.Magenta; break;
                            case "36": this.currentFormat.ForegroundColor = Colors.DarkCyan; break;
                            case "37": this.currentFormat.ForegroundColor = Colors.White; break;
                            case "39": this.currentFormat.ForegroundColor = DrawingTerminalCellFormat.DefaultForegroundColor; break;
                            case "40": this.currentFormat.BackgroundColor = Colors.Black; break;
                            case "41": this.currentFormat.BackgroundColor = Colors.Red; break;
                            case "42": this.currentFormat.BackgroundColor = Colors.Green; break;
                            case "43": this.currentFormat.BackgroundColor = Colors.Yellow; break;
                            case "44": this.currentFormat.BackgroundColor = Colors.Blue; break;
                            case "45": this.currentFormat.BackgroundColor = Colors.Magenta; break;
                            case "46": this.currentFormat.BackgroundColor = Colors.DarkCyan; break;
                            case "47": this.currentFormat.BackgroundColor = Colors.White; break;
                            case "49": this.currentFormat.BackgroundColor = DrawingTerminalCellFormat.DefaultBackgroundColor; break;
                            default:
                                break;
                        }
                    }
                    //    UpdateBrushes();
                    break;
                case "]\a":
                case "]\u001b\\":
                    // TODO: set window title
                    break;
                //case "#3":
                //case "#4":
                //case "#5":
                //case "#6":
                //    _doubleMode = (CharacterDoubling)((int)_escapeChars[1] - (int)'0');
                //    break;

                //case "[s": _saveRow = Row; _saveColumn = Column; break;
                case "7":
                    this.savedCursor = new SavedCursor(this.Display.CursorRow, this.Display.CursorColumn, this.currentFormat);
                    break;
                //case "[u": Row = _saveRow; Column = _saveColumn; break;
                case "8":
                    if (this.savedCursor == null)
                    {
                        break;
                    }

                    this.Display.CursorRow = this.savedCursor.CursorRow;
                    this.Display.CursorColumn = this.savedCursor.CursorColumn;
                    this.currentFormat = this.savedCursor.Format.Clone();
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

            if (this.Display.CursorColumn < 0) this.Display.CursorColumn = 0;

            if (this.Display.CursorColumn >= this.Columns) this.Display.CursorColumn = this.Columns - 1;
            if (this.Display.CursorRow < 0) this.Display.CursorRow = 0;
            if (this.Display.CursorRow >= this.Rows) this.Display.CursorRow = this.Rows - 1;
            this.UpdateCursor();
        }

        private void Erase(int startRow, int startColumn, int endRow, int endColumn)
        {
            DrawingTerminalDisplay display = this.Display;
            DrawingTerminalLine line;
            DrawingTerminalCell cell;
            for (int row = startRow; row <= endRow; row++)
            {
                line = display[row];
                int startColumnLine = row == startRow ? startColumn : 0;
                int endColumnLine = row == endRow ? endColumn : this.Columns - 1;
                for (int column = startColumnLine; column <= endColumnLine; column++)
                {
                    cell = line[column];

                    lock (this.Display.ChangeLock)
                    {
                        cell.Character = ' ';
                        cell.ApplyFormat(this.currentFormat);
                        this.Display.Changed = true;
                    }
                }
            }
        }

        private void NextLineWithScroll()
        {
            if (++this.Display.CursorRow > (this.scrollBottom ?? (this.Rows - 1)))
            {
                this.ScrollUp(1);

                --this.Display.CursorRow;
            }
        }

        private void PreviousLineWithScroll()
        {
            if (--this.Display.CursorRow < (this.scrollTop ?? 0))
            {
                this.ScrollDown(1);

                ++this.Display.CursorRow;
            }
        }

        private void ScrollDown(int lines)
        {
            //DrawingTerminalLine movingLine = (DrawingTerminalLine)this.Display.Lines[this.scrollBottom ?? (this.Rows - 1)];
            //lock (this.Display.ChangeLock)
            //{
            //    this.Display.Lines.RemoveAt(this.scrollBottom ?? (this.Rows - 1));
            //    foreach (DrawingTerminalCell cell in movingLine.Cells)
            //    {
            //        cell.Reset();
            //    }
            //    this.Display.Lines.Insert(this.scrollTop ?? 0, movingLine);
            //}
            lock (this.Display.ChangeLock)
            {
                for (int i = 0; i < lines; i++)
                {
                    this.Display.Lines.RemoveAt(this.scrollBottom ?? (this.Rows - 1));
                    this.Display.Lines.Insert(this.scrollTop ?? 0, new DrawingTerminalLine(this.Display));
                }

                this.Display.Changed = true;
            }
        }

        private void ScrollUp(int lines)
        {
            //DrawingTerminalLine movingLine = (DrawingTerminalLine)this.Display.Lines[this.scrollTop ?? 0];
            //lock (this.Display.ChangeLock)
            //{
            //    this.Display.Lines.RemoveAt(this.scrollTop ?? 0);
            //    foreach (DrawingTerminalCell cell in movingLine.Cells)
            //    {
            //        cell.Reset();
            //    }
            //    this.Display.Lines.Insert(this.scrollBottom ?? (this.Rows - 1), movingLine);
            //}

            lock (this.Display.ChangeLock)
            {
                for (int i = 0; i < lines; i++)
                {
                    this.Display.Lines.RemoveAt(this.scrollTop ?? 0);
                    this.Display.Lines.Insert(this.scrollBottom ?? (this.Rows - 1), new DrawingTerminalLine(this.Display));
                }

                this.Display.Changed = true;
            }
        }

        private void InsertLines(int lines)
        {
            lock (this.Display.ChangeLock)
            {
                var lastLine = this.Display[this.scrollBottom ?? (this.Rows - 1)];
                for (int i = 0; i < lines; i++)
                {
                    this.Display.Lines.RemoveAt(this.scrollBottom ?? (this.Rows - 1));
                    this.Display.Lines.Insert(this.Display.CursorRow, new DrawingTerminalLine(this.Display));
                }

                this.Display.Changed = true;
            }
        }

        private void DeleteLines(int lines)
        {
            lock (this.Display.ChangeLock)
            {
                var lastLine = this.Display[this.scrollBottom ?? (this.Rows - 1)];
                for (int i = 0; i < lines; i++)
                {
                    this.Display.Lines.RemoveAt(this.Display.CursorRow);
                    this.Display.Lines.Insert(this.scrollBottom ?? (this.Rows - 1), new DrawingTerminalLine(this.Display));
                }

                this.Display.Changed = true;
            }
        }

        private void DeleteCells(int cells)
        {
            lock (this.Display.ChangeLock)
            {
                var line = this.Display[this.Display.CursorRow];
                var lastCell = line[this.Columns - 1];
                for (int i = 0; i < cells; i++)
                {
                    line.Cells.RemoveAt(this.Display.CursorColumn);
                    line.Cells.Insert(line.Cells.Count, new DrawingTerminalCell(this.Display));
                }

                this.Display.Changed = true;
            }
        }

        private void JumpToNextHorizontalTabStop()
        {
            var previousTabStop = ((this.Display.CursorColumn / DefaultTabStopWidth) * DefaultTabStopWidth);
            var nextTabStop = previousTabStop + DefaultTabStopWidth;
            this.Display.CursorColumn = Math.Min(nextTabStop, this.Columns - 1);

            this.UpdateCursor();
        }

        public async Task WriteLineAsync(string text)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.ProcessInput(text + "\r\n"));
        }

        public async Task<string> ReadLineAsync(string prompt, bool echo)
        {
            this.localReadStartColumn = this.Display.CursorColumn;
            this.localReadStartRow = this.Display.CursorRow;
            this.localReadPrompt = prompt;

            bool oldEcho = this.LocalEcho;

            this.LocalEcho = true;
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.ProcessInput(prompt));

            this.LocalEcho = echo;
            using (this.localReadSync = new AutoResetEvent(false))
            {
                await Task.Run(() => this.localReadSync.WaitOne());
            }

            this.localReadSync = null;
            this.localReadPrompt = string.Empty;

            this.LocalEcho = oldEcho;

            if (this.localReadLine == null)
            {
                throw new InvalidOperationException("Disconnecting during connection.");
            }

            string line = this.localReadLine;
            this.localReadLine = string.Empty;
            return line;
        }

        public int Columns { get; private set; }

        public int Rows { get; private set; }

        public DrawingTerminalCell CursorCell
        {
            get
            {
                return this.Display[this.Display.CursorRow][this.Display.CursorColumn];
            }
        }

        private void AttachRenderer(int pixelWidth, int pixelHeight)
        {
            lock (this.deviceManager)
            {
                if (this.terminalRenderer != null)
                {
                    throw new InvalidOperationException("Renderer already attached.");
                }

                this.terminalRenderer = new DrawingTerminalRenderer(this.Display);
                this.d2dTarget = new SurfaceImageSourceTarget(pixelWidth, pixelHeight);

                this.deviceManager.OnInitialize += this.d2dTarget.Initialize;
                this.deviceManager.OnInitialize += this.terminalRenderer.Initialize;
                this.deviceManager.Initialize(DisplayProperties.LogicalDpi);

                this.terminalRectangle.Fill = new ImageBrush() { ImageSource = this.d2dTarget.ImageSource };
                this.d2dTarget.OnRender += terminalRenderer.Render;
                CompositionTarget.Rendering += CompositionTarget_Rendering;
            }
        }

        private void DetachRenderer()
        {
            lock (this.deviceManager)
            {
                if (this.terminalRenderer == null)
                {
                    return;
                }

                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                this.d2dTarget.OnRender -= terminalRenderer.Render;
                this.terminalRectangle.Fill = null;

                this.deviceManager.OnInitialize -= this.d2dTarget.Initialize;
                this.deviceManager.OnInitialize -= this.terminalRenderer.Initialize;

                this.d2dTarget.Dispose();
                this.d2dTarget = null;

                this.terminalRenderer.Dispose();
                this.terminalRenderer = null;
            }
        }

        public void Dispose()
        {
            if (this.connection != null)
            {
                this.connection.Dispose();
            }

            lock (this.Display.ChangeLock)
            {
                this.DetachRenderer();
            }

            this.deviceManager.Dispose();

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Automation Peer class for DrawingTerminal.  
    /// 
    /// Note: This implements Text Pattern (ITextProvider) and Value Pattern (IValuePattern) interfaces.
    /// So Touch keyboard shows automatically when user taps on the control with Touch or Pen.
    /// </summary>
    public class DrawingTerminalAutomationPeer : FrameworkElementAutomationPeer, ITextProvider, IValueProvider
    {
        private DrawingTerminal drawingTerminal;
        private string accClass = "DrawingTerminalClass";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="owner"></param>
        public DrawingTerminalAutomationPeer(DrawingTerminal owner)
            : base(owner)
        {
            this.drawingTerminal = owner;
        }

        /// <summary>
        /// Override GetPatternCore to return the object that supports the specified pattern.  In this case the Value pattern, Text
        /// patter and any base class patterns.
        /// </summary>
        /// <param name="patternInterface"></param>
        /// <returns>the object that supports the specified pattern</returns>
        protected override object GetPatternCore(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
            {
                return this;
            }
            else if (patternInterface == PatternInterface.Text)
            {
                return this;
            }
            return base.GetPatternCore(patternInterface);
        }

        /// <summary>
        /// Override GetClassNameCore and set the name of the class that defines the type associated with this control.
        /// </summary>
        /// <returns>The name of the control class</returns>
        protected override string GetClassNameCore()
        {
            return this.accClass;
        }

        #region Implementation for ITextPattern interface
        // Complete implementation of the ITextPattern is beyond the scope of this sample.  The implementation provided
        // is specific to this sample's custom control, so it is unlikely that they are directly transferable to other 
        // custom control.

        ITextRangeProvider ITextProvider.DocumentRange
        {
            // A real implementation of this method is beyond the scope of this sample.
            // If your custom control has complex text involving both readonly and non-readonly ranges, 
            // it will need a smarter implementation than just returning a fixed range
            get
            {
                //return new DrawingTerminalRangeProvider(terminal.ContentText, this); ;
                return new DrawingTerminalRangeProvider(string.Empty, this); ;
            }
        }

        ITextRangeProvider[] ITextProvider.GetSelection()
        {
            return new ITextRangeProvider[0];
        }

        ITextRangeProvider[] ITextProvider.GetVisibleRanges()
        {
            ITextRangeProvider[] ret = new ITextRangeProvider[1];
            //ret[0] = new DrawingTerminalRangeProvider(terminal.ContentText, this);
            ret[0] = new DrawingTerminalRangeProvider(string.Empty, this);
            return ret;
        }

        ITextRangeProvider ITextProvider.RangeFromChild(IRawElementProviderSimple childElement)
        {
            //return new DrawingTerminalRangeProvider(terminal.ContentText, this);
            return new DrawingTerminalRangeProvider(string.Empty, this);
        }

        ITextRangeProvider ITextProvider.RangeFromPoint(Point screenLocation)
        {
            //return new DrawingTerminalRangeProvider(terminal.ContentText, this);
            return new DrawingTerminalRangeProvider(string.Empty, this);
        }

        SupportedTextSelection ITextProvider.SupportedTextSelection
        {
            get { return SupportedTextSelection.None; }
        }

        #endregion

        #region Implementation for IValueProvider interface
        // Complete implementation of the IValueProvider is beyond the scope of this sample.  The implementation provided
        // is specific to this sample's custom control, so it is unlikely that they are directly transferable to other 
        // custom control.

        /// <summary>
        /// The value needs to be false for the Touch keyboard to be launched automatically because Touch keyboard
        /// does not appear when the input focus is in a readonly UI control.
        /// </summary>
        bool IValueProvider.IsReadOnly
        {
            get { return false; }
        }

        void IValueProvider.SetValue(string value)
        {
            //terminal.ContentText = value;
            return;
        }

        string IValueProvider.Value
        {
            get
            {
                //return terminal.ContentText;
                return string.Empty;
            }
        }

        #endregion //Implementation for IValueProvider interface

        public IRawElementProviderSimple GetRawElementProviderSimple()
        {
            return ProviderFromPeer(this);
        }
    }

    /// <summary>
    /// A minimal implementation of ITextRangeProvider, used by DrawingTerminalAutomationPeer
    /// A real implementation is beyond the scope of this sample
    /// </summary>
    public sealed class DrawingTerminalRangeProvider : ITextRangeProvider
    {
        private String _text;
        private DrawingTerminalAutomationPeer _peer;

        public DrawingTerminalRangeProvider(String text, DrawingTerminalAutomationPeer peer)
        {
            _text = text;
            _peer = peer;
        }

        public void AddToSelection()
        {

        }

        public ITextRangeProvider Clone()
        {
            return new DrawingTerminalRangeProvider(_text, _peer);
        }

        public bool Compare(ITextRangeProvider other)
        {
            return true;
        }

        public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            return 0;
        }

        public void ExpandToEnclosingUnit(TextUnit unit)
        {

        }

        public ITextRangeProvider FindAttribute(int attribute, Object value, bool backward)
        {
            return this;
        }

        public ITextRangeProvider FindText(String text, bool backward, bool ignoreCase)
        {
            return this;
        }

        public Object GetAttributeValue(int attribute)
        {
            return this;
        }

        public void GetBoundingRectangles(out double[] rectangles)
        {
            rectangles = new double[0];
        }

        public IRawElementProviderSimple[] GetChildren()
        {
            return new IRawElementProviderSimple[0];
        }

        public IRawElementProviderSimple GetEnclosingElement()
        {
            return _peer.GetRawElementProviderSimple();
        }

        public String GetText(int maxLength)
        {
            return (maxLength < 0) ? _text : _text.Substring(0, Math.Min(_text.Length, maxLength));
        }

        public int Move(TextUnit unit, int count)
        {
            return 0;
        }

        public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {

        }

        public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            return 0;
        }

        public void RemoveFromSelection()
        {

        }

        public void ScrollIntoView(bool alignToTop)
        {

        }

        public void Select()
        {

        }
    }
}
