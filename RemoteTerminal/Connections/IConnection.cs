using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteTerminal.Model;
using RemoteTerminal.Terminals;

namespace RemoteTerminal.Connections
{
    public interface IConnection : IDisposable
    {
        void Initialize(ConnectionData connectionData);
        Task<bool> ConnectAsync(ITerminal terminal);
        void Disconnect();
        Task<string> ReadAsync();
        void Write(string str);
        void ResizeTerminal(int columns, int rows);
    }
}
