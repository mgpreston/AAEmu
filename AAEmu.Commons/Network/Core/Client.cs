using System.Net;
using TcpClient = NetCoreServer.TcpClient;

namespace AAEmu.Commons.Network.Core;

public class Client(IPAddress serverAddress, int serverPort, BaseProtocolHandler handler)
    : TcpClient(serverAddress, serverPort), ISession
{
    private readonly Dictionary<string, object> _attributes = [];
    private uint _sessionId;
    private IPAddress _ip;

    IPAddress ISession.Ip => _ip;

    uint ISession.SessionId => _sessionId;

    void ISession.SendPacket(ReadOnlySpan<byte> packet) => SendAsync(packet);

    public ValueTask SendAsync(PacketStream packet, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public bool TrySend(PacketStream packet)
    {
        throw new NotImplementedException();
    }

    void ISession.AddAttribute(string name, object attribute) => _attributes.Add(name, attribute);

    object ISession.GetAttribute(string name)
    {
        _attributes.TryGetValue(name, out var attribute);
        return attribute;
    }

    void ISession.ClearAttribute(string name) => _attributes.Remove(name);

    void ISession.Close() => Disconnect();

    protected override void OnConnected()
    {
        _sessionId = (uint)Socket.LocalEndPoint.GetHashCode();
        _ip = ((IPEndPoint)Socket.LocalEndPoint).Address;
        handler.OnConnect(this);
    }

    protected override void OnDisconnected() => handler.OnDisconnect(this);

    protected override void OnReceived(byte[] buffer, long offset, long size) =>
        handler.OnReceive(this, buffer, (int)offset, (int)size);
}
