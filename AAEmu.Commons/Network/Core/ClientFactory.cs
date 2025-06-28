#nullable enable
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace AAEmu.Commons.Network.Core;

public class ClientFactory(ClientOptions clientOptions, ILoggerFactory loggerFactory) : IClientFactory
{
    public IClient2 Create(TcpClient tcpClient, IBaseProtocolHandler protocolHandler) =>
        new Client2(tcpClient, protocolHandler, clientOptions, loggerFactory.CreateLogger<Client2>());
}
