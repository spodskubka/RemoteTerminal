using System;
using System.ComponentModel;
using RemoteTerminal.Screens;
using Windows.System;

namespace RemoteTerminal.Terminals
{
    /// <summary>
    /// Represents a terminal.
    /// </summary>
    public interface ITerminal : IDisposable, INotifyPropertyChanged
    {
        IRenderableScreen RenderableScreen { get; }

        void PowerOn();
        void PowerOff();
        void ResizeScreen(int rows, int columns);

        void ProcessKeyPress(char ch);
        bool ProcessKeyPress(VirtualKey key, KeyModifiers keyModifiers);
        void ProcessPastedText(string str);
        bool ScreenHasFocus { set; }

        bool IsConnected { get; }
        event EventHandler Disconnected;

        string Name { get; }
        string Title { get; }
    }
}
