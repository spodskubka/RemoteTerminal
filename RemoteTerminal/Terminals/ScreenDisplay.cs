using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonDX;
using RemoteTerminal.Connections;
using RemoteTerminal.Model;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Automation.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace RemoteTerminal.Terminals
{
    public sealed class ScreenDisplay : UserControl, IDisposable
    {
        private ITerminal terminal = null;

        private readonly Rectangle rectangle;
        private readonly Border border;

        // DirectX stuff
        private readonly DeviceManager deviceManager;
        private ScreenDisplayRenderer terminalRenderer = null;
        private SurfaceImageSourceTarget d2dTarget = null;
        private bool firstRender = true;

        public ScreenDisplay()
        {
            this.rectangle = new Rectangle();
            this.border = new Border()
            {
                Child = this.rectangle
            };

            this.border.BorderBrush = new SolidColorBrush(Colors.Gray);
            this.border.BorderThickness = new Thickness(2d);
            this.border.Background = new SolidColorBrush(Colors.Black);

            this.Content = border;

            this.IsTabStop = true;
            this.IsTapEnabled = true;

            this.deviceManager = new DeviceManager();
        }

        public static double TerminalCellFontSize { get { return 17d; } }
        public static double TerminalCellWidth { get { return 9d; } }
        public static double TerminalCellHeight { get { return 20d; } }

        public Color CursorForegroundColor { get { return Colors.Black; } }
        public Color CursorBackgroundColor { get { return Colors.Green; } }

        public void AssignTerminal(Guid terminalGuid)
        {
            if (this.terminal != null)
            {
                this.DetachRenderer();
                this.terminal.Dispose();
                this.terminal = null;
            }

            ITerminal terminal = TerminalManager.GetActive(terminalGuid);
            this.terminal = terminal;
            this.terminal.Disconnected += terminal_Disconnected;

            this.border.BorderBrush = new SolidColorBrush(this.terminal.IsConnected ? Colors.Gray : Colors.Red);
            this.IsEnabled = this.terminal.IsConnected;

            // This will result in ArrangeOverride being called, where the new renderer is attached.
            this.InvalidateArrange();
        }

        async void terminal_Disconnected(object sender, EventArgs e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                this.border.BorderBrush = new SolidColorBrush(Colors.Red);
                this.IsEnabled = false;
            });
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            int rows = (int)((finalSize.Height - 4) / ScreenDisplay.TerminalCellHeight);
            int columns = (int)((finalSize.Width - 4) / ScreenDisplay.TerminalCellWidth);

            if (this.terminal == null)
            {
                return base.ArrangeOverride(finalSize);
            }

            double terminalRectangleWidth = finalSize.Width - this.border.BorderThickness.Left - this.border.BorderThickness.Right;
            double terminalRectangleHeight = finalSize.Height - this.border.BorderThickness.Top - this.border.BorderThickness.Bottom;
            int pixelWidth = (int)(terminalRectangleWidth * DisplayProperties.LogicalDpi / 96.0);
            int pixelHeight = (int)(terminalRectangleHeight * DisplayProperties.LogicalDpi / 96.0);

            this.DetachRenderer();
            this.terminal.ResizeScreen(rows, columns);
            this.AttachRenderer(pixelWidth, pixelHeight);

            return base.ArrangeOverride(finalSize);
        }

        void CompositionTarget_Rendering(object sender, object e)
        {
            if (!this.terminal.RenderableScreen.Changed && !this.firstRender)
            {
                return;
            }

            lock (this.deviceManager)
            {
                if (this.d2dTarget == null)
                {
                    return;
                }

                this.d2dTarget.RenderAll();
                this.firstRender = false;
            }
        }

        /// <summary>
        /// Create the Automation peer implementations for ScreenDisplay to provide the accessibility support.
        /// </summary>
        /// <returns>Automation Peer implementation for this control</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ScreenDisplayAutomationPeer(this);
        }

        /// <summary>
        /// Override the default event handler for GotFocus.
        /// When the control got focus, indicate it has focus by highlighting the control by changing the background color to yellow.
        /// </summary>
        /// <param name="e">State information and event data associated with GotFocus event.</param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            CoreWindow.GetForCurrentThread().CharacterReceived += Terminal_CharacterReceived;
            this.terminal.ScreenHasFocus = true;
        }

        /// <summary>
        /// Override the default event handler for LostFocus.
        /// When the control lost focus, indicate it does not have focus by changing the background color to gray.
        /// And the content is cleared.
        /// </summary>
        /// <param name="e">State information and event data associated with LostFocus event.</param>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            CoreWindow.GetForCurrentThread().CharacterReceived -= Terminal_CharacterReceived;
            this.terminal.ScreenHasFocus = false;
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
        void Terminal_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            // This method receives all input that represents "characters".
            // It does not receive: Return, Cursor keys (Up, Down, Left, Right), Tabulator, Function keys (F1 - F12), 
            this.terminal.ProcessKeyPress((char)args.KeyCode);
            args.Handled = true;
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            var keyModifiers = KeyModifiers.None;

            var coreWindow = Window.Current.CoreWindow;
            keyModifiers |= coreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) ? KeyModifiers.Shift : KeyModifiers.None;
            keyModifiers |= coreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down) ? KeyModifiers.Ctrl : KeyModifiers.None;
            keyModifiers |= coreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down) ? KeyModifiers.Alt : KeyModifiers.None;

            e.Handled = this.terminal.ProcessKeyPress(e.Key, keyModifiers);
        }

        private void AttachRenderer(int pixelWidth, int pixelHeight)
        {
            lock (this.deviceManager)
            {
                if (this.terminalRenderer != null)
                {
                    throw new InvalidOperationException("Renderer already attached.");
                }

                this.terminalRenderer = new ScreenDisplayRenderer(this, this.terminal.RenderableScreen);
                this.d2dTarget = new SurfaceImageSourceTarget(pixelWidth, pixelHeight);
                this.firstRender = true;

                this.deviceManager.OnInitialize += this.d2dTarget.Initialize;
                this.deviceManager.OnInitialize += this.terminalRenderer.Initialize;
                this.deviceManager.Initialize(DisplayProperties.LogicalDpi);

                this.rectangle.Fill = new ImageBrush() { ImageSource = this.d2dTarget.ImageSource };
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
                this.rectangle.Fill = null;

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
            this.DetachRenderer();
            if (this.terminal != null)
            {
                this.terminal.Disconnected -= terminal_Disconnected;
                this.terminal.PowerOff();
                this.terminal.Dispose();
            }

            this.deviceManager.Dispose();

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Automation Peer class for ScreenDisplay.  
    /// 
    /// Note: This implements Text Pattern (ITextProvider) and Value Pattern (IValuePattern) interfaces.
    /// So Touch keyboard shows automatically when user taps on the control with Touch or Pen.
    /// </summary>
    public class ScreenDisplayAutomationPeer : FrameworkElementAutomationPeer, ITextProvider, IValueProvider
    {
        private ScreenDisplay screenDisplay;
        private string accClass = "ScreenDisplayClass";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="owner"></param>
        public ScreenDisplayAutomationPeer(ScreenDisplay owner)
            : base(owner)
        {
            this.screenDisplay = owner;
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
                //return new ScreenDisplayRangeProvider(terminal.ContentText, this); ;
                return new ScreenDisplayRangeProvider(string.Empty, this); ;
            }
        }

        ITextRangeProvider[] ITextProvider.GetSelection()
        {
            return new ITextRangeProvider[0];
        }

        ITextRangeProvider[] ITextProvider.GetVisibleRanges()
        {
            ITextRangeProvider[] ret = new ITextRangeProvider[1];
            //ret[0] = new ScreenDisplayRangeProvider(terminal.ContentText, this);
            ret[0] = new ScreenDisplayRangeProvider(string.Empty, this);
            return ret;
        }

        ITextRangeProvider ITextProvider.RangeFromChild(IRawElementProviderSimple childElement)
        {
            //return new ScreenDisplayRangeProvider(terminal.ContentText, this);
            return new ScreenDisplayRangeProvider(string.Empty, this);
        }

        ITextRangeProvider ITextProvider.RangeFromPoint(Point screenLocation)
        {
            //return new ScreenDisplayRangeProvider(terminal.ContentText, this);
            return new ScreenDisplayRangeProvider(string.Empty, this);
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
    /// A minimal implementation of ITextRangeProvider, used by ScreenDisplayAutomationPeer
    /// A real implementation is beyond the scope of this sample
    /// </summary>
    public sealed class ScreenDisplayRangeProvider : ITextRangeProvider
    {
        private String _text;
        private ScreenDisplayAutomationPeer _peer;

        public ScreenDisplayRangeProvider(String text, ScreenDisplayAutomationPeer peer)
        {
            _text = text;
            _peer = peer;
        }

        public void AddToSelection()
        {

        }

        public ITextRangeProvider Clone()
        {
            return new ScreenDisplayRangeProvider(_text, _peer);
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
