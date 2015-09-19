using System.Collections.Generic;

namespace RemoteTerminal.Screens
{
    /// <summary>
    /// The virtual in-memory representation of a screen line.
    /// </summary>
    public class ScreenLine : List<ScreenCell>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenLine"/> class with the specified number of columns.
        /// </summary>
        /// <param name="columns">The number of columns.</param>
        /// <remarks>
        /// Gets cells from the cell recycler in the <see cref="ScreenCell"/> class.
        /// </remarks>
        public ScreenLine(int columns)
            : base(columns)
        {
            this.AddRange(ScreenCell.GetFreshCells(columns));
        }
    }
}
