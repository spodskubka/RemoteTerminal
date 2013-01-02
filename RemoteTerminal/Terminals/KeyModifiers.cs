using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTerminal.Terminals
{
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Shift = 1,
        Ctrl = 1 << 1,
        Alt = 1 << 2,
    }
}
