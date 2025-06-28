#nullable enable
using System.Net.Sockets;

namespace AAEmu.Commons.Network.Core;

public interface IClientFactory
{
    IClient2 Create(TcpClient tcpClient, IBaseProtocolHandler protocolHandler);
}
