using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteTerminal.Screens
{
    public interface IRenderableScreen
    {
        bool Changed { get; }
        IRenderableScreenCopy GetScreenCopy();
    }
}
