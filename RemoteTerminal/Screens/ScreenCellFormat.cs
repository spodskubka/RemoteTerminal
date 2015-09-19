namespace RemoteTerminal.Screens
{
    /// <summary>
    /// Describes the format of a screen cell.
    /// </summary>
    public class ScreenCellFormat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenCellFormat"/> class.
        /// </summary>
        public ScreenCellFormat()
        {
            this.Reset();
        }

        /// <summary>
        /// Gets or sets the foreground color.
        /// </summary>
        public ScreenColor ForegroundColor { get; set; }

        /// <summary>
        /// Gets or sets the background color.
        /// </summary>
        public ScreenColor BackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the text should be bold.
        /// </summary>
        public bool BoldMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the text should be underlined.
        /// </summary>
        public bool UnderlineMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the foreground and background colors should be reversed.
        /// </summary>
        public bool ReverseMode { get; set; }

        /// <summary>
        /// Resets the state of this object to a newly created one.
        /// </summary>
        public void Reset()
        {
            this.ForegroundColor = ScreenColor.DefaultForeground;
            this.BackgroundColor = ScreenColor.DefaultBackground;
            this.BoldMode = false;
            this.UnderlineMode = false;
            this.ReverseMode = false;
        }

        /// <summary>
        /// Clones the screen cell format.
        /// </summary>
        /// <returns>The cloned screen cell format.</returns>
        public ScreenCellFormat Clone()
        {
            return new ScreenCellFormat()
            {
                ForegroundColor = this.ForegroundColor,
                BackgroundColor = this.BackgroundColor,
                BoldMode = this.BoldMode,
                UnderlineMode = this.UnderlineMode,
                ReverseMode = this.ReverseMode,
            };
        }
    }
}
