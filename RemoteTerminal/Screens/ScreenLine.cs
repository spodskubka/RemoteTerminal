using System.Collections.Generic;

namespace RemoteTerminal.Screens
{
    public class ScreenLine : List<ScreenCell>
    {
        public ScreenLine(int columns)
            : base(columns)
        {
            this.AddRange(ScreenCell.GetFreshCells(columns));
        }
    }
}
