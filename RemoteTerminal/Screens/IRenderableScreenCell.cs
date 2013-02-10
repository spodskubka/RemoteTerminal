using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace RemoteTerminal.Screens
{
    public interface IRenderableScreenCell
    {
        char Character { get; }
        ScreenCellModifications Modifications { get; }
        ScreenColor ForegroundColor { get; }
        ScreenColor BackgroundColor { get; }
    }
}
