using System;
using System.Collections.Generic;
using CommonDX;
using RemoteTerminal.Model;
using RemoteTerminal.Screens;
using Windows.ApplicationModel.DataTransfer;
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
using Windows.UI.Xaml.Shapes;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// This is a XAML user control for displaying a terminal screen.
    /// It does not only display a terminal screen but it also forwards user input and screen size changes to the terminal.
    /// </summary>
    /// <remarks>
    /// The terminal screen is drawn via a <see cref="ScreenDisplayRenderer"/>, which uses DirectX (DirectWrite).
    /// </remarks>
    public sealed class ScreenDisplay : UserControl, IDisposable
    {
        /// <summary>
        /// The terminal associated with this control.
        /// </summary>
        private ITerminal terminal = null;

        /// <summary>
        /// A rectangle shape that is part of this control's content.
        /// </summary>
        private readonly Rectangle rectangle;

        /// <summary>
        /// A border that is part of this control's content.
        /// </summary>
        private readonly Border border;

        /// <summary>
        /// The DirectX device manager.
        /// </summary>
        private readonly DeviceManager deviceManager;

        /// <summary>
        /// The renderer that draws the terminal screen onto this control.
        /// </summary>
        private ScreenDisplayRenderer terminalRenderer = null;

        /// <summary>
        /// The Direct2D drawing target.
        /// </summary>
        private SurfaceImageSourceTarget d2dTarget = null;

        /// <summary>
        /// A value indicating whether the screen should be rendered regardless of whether it has changed or not.
        /// </summary>
        /// <remarks>
        /// This is used to force rendering after font or theme changes.
        /// </remarks>
        private bool forceRender = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenDisplay"/> class.
        /// </summary>
        public ScreenDisplay()
        {
            this.rectangle = new Rectangle();
            this.border = new Border()
            {
                Child = this.rectangle
            };

            this.border.BorderBrush = new SolidColorBrush(Colors.Black);
            this.border.BorderThickness = new Thickness(2d);
            this.border.Background = new SolidColorBrush(Colors.Black);

            this.Content = border;

            this.IsTabStop = true;
            this.IsTapEnabled = true;
            this.ManipulationMode = ManipulationModes.TranslateY | ManipulationModes.TranslateInertia;

            this.ColorTheme = ColorThemeData.CreateDefault();
            this.RecalculateFontMetrics();

            this.deviceManager = new DeviceManager();
        }

        /// <summary>
        /// The theme to use for drawing the screen.
        /// </summary>
        private ColorThemeData colorTheme;

        /// <summary>
        /// Gets or sets the theme to use for drawing the screen.
        /// </summary>
        public ColorThemeData ColorTheme
        {
            get
            {
                return this.colorTheme;
            }
            set
            {
                this.colorTheme = value; ForceRender(true);
            }
        }

        /// <summary>
        /// The scaling factor that is used when the font size is changed in a theme.
        /// </summary>
        public const float FontSizeScalingFactor = 0.1f;

        /// <summary>
        /// The logical base font metrics for all supported fonts.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, ScreenFontMetrics> BaseLogicalFontMetrics = new Dictionary<string, ScreenFontMetrics>()
        {
          { "Consolas", new ScreenFontMetrics(fontSize: 17.0f, cellWidth: 9.35f, cellHeight: 20.0f) },
          { "Courier New", new ScreenFontMetrics(fontSize: 17.0f, cellWidth: 10.21f, cellHeight: 20.0f) },
          { "KaiTi", new ScreenFontMetrics(fontSize: 17.0f, cellWidth: 8.51f, cellHeight: 20.0f) },
          { "SimSun", new ScreenFontMetrics(fontSize: 17.0f, cellWidth: 8.5f, cellHeight: 20.0f) },
          //{ "Courier", new ScreenFontMetrics(fontSize: 17.0f, cellWidth: 9.45f, cellHeight: 20.0f) },
          //{ "Fixedsys", new ScreenFontMetrics(fontSize: 17.0f, cellWidth: 9.35f, cellHeight: 20.0f) },
          //{ "Lucida Console", new ScreenFontMetrics(fontSize: 17.0f, cellWidth: 10.26f, cellHeight: 20.0f) },
          //{ "Terminal", new ScreenFontMetrics(fontSize: 17.0f, cellWidth: 9.35f, cellHeight: 20.0f) },
        };

        /// <summary>
        /// Gets the logical font metrics to use for drawing the screen.
        /// </summary>
        public ScreenFontMetrics FontMetrics { get; private set; }

        /// <summary>
        /// Recalculates the current logical font metrics based on the selected font family and size.
        /// </summary>
        private void RecalculateFontMetrics()
        {
            this.FontMetrics = BaseLogicalFontMetrics[this.ColorTheme.FontFamily] * (1 + (FontSizeScalingFactor * (float)this.ColorTheme.FontSize));
        }

        /// <summary>
        /// Holds the amount of scrolled pixels during scrolling via keyboard, mouse wheel or touch.
        /// </summary>
        private double scroller = 0d;

        /// <summary>
        /// Assigns a terminal to this screen display.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        public void AssignTerminal(ITerminal terminal)
        {
            if (this.terminal != null)
            {
                this.DetachRenderer();
                this.terminal.Disconnected -= terminal_Disconnected;
                this.terminal = null;
            }

            this.terminal = terminal;

            if (this.terminal == null)
            {
                return;
            }

            this.terminal.Disconnected += terminal_Disconnected;

            this.border.BorderBrush = new SolidColorBrush(this.terminal.IsConnected ? Colors.Black : Colors.Red);

            // This will result in ArrangeOverride being called, where the new renderer is attached.
            this.InvalidateArrange();
        }

        /// <summary>
        /// Occurs when the terminal's connection is disconnected.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains no event data.</param>
        async void terminal_Disconnected(object sender, EventArgs e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                this.border.BorderBrush = new SolidColorBrush(Colors.Red);
            });
        }

        /// <summary>
        /// Provides the behavior for the Arrange pass of layout.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.terminal == null)
            {
                return base.ArrangeOverride(finalSize);
            }

            double terminalRectangleWidth = finalSize.Width - this.border.BorderThickness.Left - this.border.BorderThickness.Right;
            double terminalRectangleHeight = finalSize.Height - this.border.BorderThickness.Top - this.border.BorderThickness.Bottom;

            this.DetachRenderer();

            float logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;

            int pixelWidth = (int)(terminalRectangleWidth * logicalDpi / 96.0);
            int pixelHeight = (int)(terminalRectangleHeight * logicalDpi / 96.0);

            int rows = (int)(pixelHeight / (this.FontMetrics.CellHeight * logicalDpi / 96.0));
            int columns = (int)(pixelWidth / (this.FontMetrics.CellWidth * logicalDpi / 96.0));
            this.terminal.ResizeScreen(rows, columns);

            this.AttachRenderer(pixelWidth, pixelHeight);

            // The following two lines are a workaround for the fact that the ImageBrush is not displayed
            // correctly after opening the page when using non-standard DPI
            this.DetachRenderer();
            this.AttachRenderer(pixelWidth, pixelHeight);

            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// Occurs when the core rendering process renders a frame.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        void CompositionTarget_Rendering(object sender, object e)
        {
            if (!this.terminal.RenderableScreen.Changed && !this.forceRender)
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
                this.forceRender = false;
            }
        }

        /// <summary>
        /// Forces rendering of the terminal screen.
        /// </summary>
        /// <param name="fontChanged">A value indicating whether the font family or size has changed (results in font metric recalculation).</param>
        public void ForceRender(bool fontChanged)
        {
            if (fontChanged)
            {
                this.RecalculateFontMetrics();
                this.InvalidateArrange();
            }
            else
            {
                this.forceRender = true;
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
            if (this.terminal != null)
            {
                this.terminal.ScreenHasFocus = true;
            }
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

            if (this.terminal != null)
            {
                this.terminal.ScreenHasFocus = false;
            }

            // if the focus was lost during scrolling (unclear why, it just happens) get the focus back automatically
            if (this.scroller != 0d)
            {
                this.Focus(Windows.UI.Xaml.FocusState.Programmatic);
                return;
            }
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
        /// Receives user input from the system.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event data.</param>
        void Terminal_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (!this.terminal.IsConnected)
            {
                return;
            }

            // This method receives all input that represents "characters".
            // It does not receive: Return, Cursor keys (Up, Down, Left, Right), Tabulator, Function keys (F1 - F12), 
            this.terminal.ProcessKeyPress((char)args.KeyCode);
            args.Handled = true;
        }

        /// <summary>
        /// Override the default event handler for KeyDown.
        /// </summary>
        /// <param name="e">State information and event data associated with KeyDown event.</param>
        /// <remarks>
        /// Handles Scrolling via Shift-PageUp or Shift-PageDown as well as paste functionality via Shift-Insert.
        /// Everything else is sent to the terminal.
        /// </remarks>
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            var keyModifiers = KeyModifiers.None;

            var coreWindow = Window.Current.CoreWindow;
            keyModifiers |= coreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) ? KeyModifiers.Shift : KeyModifiers.None;
            keyModifiers |= coreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down) ? KeyModifiers.Ctrl : KeyModifiers.None;
            keyModifiers |= coreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down) ? KeyModifiers.Alt : KeyModifiers.None;

            if (keyModifiers == KeyModifiers.Shift)
            {
                if (e.Key == VirtualKey.PageUp || e.Key == VirtualKey.PageDown)
                {
                    this.scroller = this.FontMetrics.CellHeight * (this.terminal.RenderableScreen.RowCount / 2);
                    this.scroller *= e.Key == VirtualKey.PageUp ? 1d : -1d;
                    ProcessScroller();
                    this.scroller = 0d;
                    e.Handled = true;
                    return;
                }
                else if (e.Key == VirtualKey.Insert)
                {
                    if (!this.terminal.IsConnected)
                    {
                        return;
                    }

                    var clipboardContent = Clipboard.GetContent();
                    if (!clipboardContent.Contains(StandardDataFormats.Text))
                    {
                        return;
                    }

                    string text = clipboardContent.GetTextAsync().AsTask().Result;
                    this.terminal.ProcessPastedText(text);
                    return;
                }
            }

            if (!this.terminal.IsConnected)
            {
                return;
            }

            e.Handled = this.terminal.ProcessKeyPress(e.Key, keyModifiers);
        }

        /// <summary>
        /// Called before the ManipulationStarting event occurs.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        /// <remarks>
        /// This is used for touch scrolling.
        /// </remarks>
        protected override void OnManipulationStarting(ManipulationStartingRoutedEventArgs e)
        {
            base.OnManipulationStarting(e);
        }

        /// <summary>
        /// Called before the ManipulationStarted event occurs.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        /// <remarks>
        /// This is used for touch scrolling.
        /// </remarks>
        protected override void OnManipulationStarted(ManipulationStartedRoutedEventArgs e)
        {
            this.scroller = 0d;
            e.Handled = true;

            base.OnManipulationStarted(e);
        }

        /// <summary>
        /// Called before the ManipulationInertiaStarting event occurs.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        /// <remarks>
        /// This is used for touch scrolling.
        /// </remarks>
        protected override void OnManipulationInertiaStarting(ManipulationInertiaStartingRoutedEventArgs e)
        {
            this.scroller += e.Delta.Translation.Y;
            ProcessScroller();
            e.Handled = true;

            base.OnManipulationInertiaStarting(e);
        }

        /// <summary>
        /// Called before the ManipulationDelta event occurs.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        /// <remarks>
        /// This is used for touch scrolling.
        /// </remarks>
        protected override void OnManipulationDelta(ManipulationDeltaRoutedEventArgs e)
        {
            this.scroller += e.Delta.Translation.Y;
            e.Handled = true;
            if (ProcessScroller() && e.IsInertial)
            {
                e.Complete();
            }

            base.OnManipulationDelta(e);
        }

        /// <summary>
        /// Called before the ManipulationCompleted event occurs.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        /// <remarks>
        /// This is used for touch scrolling.
        /// </remarks>
        protected override void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs e)
        {
            this.scroller = 0d;

            base.OnManipulationCompleted(e);
        }

        /// <summary>
        /// Called before the PointerWheelChanged event occurs.
        /// </summary>
        /// <param name="e">Event data for the event.</param>
        /// <remarks>
        /// This is used for mouse wheel scrolling.
        /// </remarks>
        protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
        {
            var mouseProperties = e.GetCurrentPoint(null).Properties;
            if (!mouseProperties.IsHorizontalMouseWheel)
            {
                this.scroller += mouseProperties.MouseWheelDelta;
                ProcessScroller();
                e.Handled = true;
                this.scroller = 0d;
            }

            base.OnPointerWheelChanged(e);
        }

        /// <summary>
        /// Processes scrolling.
        /// </summary>
        /// <returns>true if a scrolling boundary was reached.</returns>
        private bool ProcessScroller()
        {
            if (Math.Abs(this.scroller) > this.FontMetrics.CellHeight)
            {
                int scrollRows = (int)(this.scroller / this.FontMetrics.CellHeight);
                this.scroller -= scrollRows * this.FontMetrics.CellHeight;

                IRenderableScreen screen = this.terminal.RenderableScreen;
                int scrollRowsCalculated = scrollRows;
                scrollRows = Math.Max(scrollRows, 0 - screen.ScrollbackPosition);
                scrollRows = Math.Min(scrollRows, screen.ScrollbackRowCount - screen.ScrollbackPosition);
                this.terminal.RenderableScreen.ScrollbackPosition += scrollRows;

                return scrollRows != scrollRowsCalculated;
            }

            return false;
        }

        /// <summary>
        /// Attaches a renderer.
        /// </summary>
        /// <param name="pixelWidth">The width of the display area in pixels.</param>
        /// <param name="pixelHeight">The height of the display area in pixels.</param>
        /// <exception cref="InvalidOperationException">A renderer is already attached.</exception>
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
                this.forceRender = true;

                this.deviceManager.OnInitialize += this.d2dTarget.Initialize;
                this.deviceManager.OnInitialize += this.terminalRenderer.Initialize;
                this.deviceManager.Initialize(DisplayInformation.GetForCurrentView().LogicalDpi);

                this.rectangle.Fill = new ImageBrush() { ImageSource = this.d2dTarget.ImageSource };
                this.d2dTarget.OnRender += terminalRenderer.Render;
                CompositionTarget.Rendering += CompositionTarget_Rendering;
            }
        }

        /// <summary>
        /// Detaches the renderer.
        /// </summary>
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.DetachRenderer();
            if (this.terminal != null)
            {
                this.terminal.Disconnected -= terminal_Disconnected;
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
    /// <remarks>
    /// This was taken from a Windows Store apps sample code from Microsoft:
    /// https://code.msdn.microsoft.com/windowsapps/Touch-keyboard-sample-43532fda
    /// </remarks>
    public class ScreenDisplayAutomationPeer : FrameworkElementAutomationPeer, ITextProvider, IValueProvider
    {
        /// <summary>
        /// The screen display control that owns this automation peer.
        /// </summary>
        private ScreenDisplay screenDisplay;

        /// <summary>
        /// The name of the control class.
        /// </summary>
        private string accClass = "ScreenDisplayClass";

        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenDisplayAutomationPeer"/> class.
        /// </summary>
        /// <param name="owner">The screen display control that owns this automation peer.</param>
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
    /// <remarks>
    /// This was taken from a Windows Store apps sample code from Microsoft:
    /// https://code.msdn.microsoft.com/windowsapps/Touch-keyboard-sample-43532fda
    /// </remarks>
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
