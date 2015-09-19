using System;
using System.Threading.Tasks;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;

namespace RemoteTerminal.Connections
{
    public interface IConnection : IDisposable
    {
        bool IsConnected { get; }
        void Initialize(ConnectionData connectionData);
        Task<bool> ConnectAsync(IConnectionInitializingTerminal terminal);
        Task<string> ReadAsync();
        void Write(string str);
        void ResizeTerminal(int rows, int columns);
        void Disconnect();
    }
}
