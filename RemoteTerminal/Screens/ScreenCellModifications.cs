using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTerminal.Screens
{
    [Flags]
    public enum ScreenCellModifications
    {
        None = 0,
        Bold = 1,
        Underline = 1 << 1,
    }
}
