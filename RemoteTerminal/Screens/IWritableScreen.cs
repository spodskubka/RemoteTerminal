using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTerminal.Screens
{
    public interface IWritableScreen : IRenderableScreen
    {
        IScreenModifier GetModifier();
        int CursorRow { get; }
        int CursorColumn { get; }
    }
}
