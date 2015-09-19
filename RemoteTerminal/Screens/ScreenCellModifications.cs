using System;

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
