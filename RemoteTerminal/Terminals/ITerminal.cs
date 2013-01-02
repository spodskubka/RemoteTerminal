using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteTerminal.Connections;
using RemoteTerminal.Model;
using RemoteTerminal.Screens;
using Windows.System;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// Represents a terminal.
    /// </summary>
    public interface ITerminal : IDisposable
    {
        IRenderableScreen RenderableScreen { get; }

        void PowerOn();
        void PowerOff();
        void ResizeScreen(int rows, int columns);

        void ProcessKeyPress(char ch);
        bool ProcessKeyPress(VirtualKey key, KeyModifiers keyModifiers);
        bool ScreenHasFocus { set; }

        bool IsConnected { get; }
        event EventHandler Disconnected;
    }
}
