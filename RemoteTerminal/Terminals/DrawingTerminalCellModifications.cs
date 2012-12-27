using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTerminal.Terminals
{
    [Flags]
    public enum DrawingTerminalCellModifications
    {
        None = 0,
        Bold = 1,
        Underline = 1 << 1,
    }
}
