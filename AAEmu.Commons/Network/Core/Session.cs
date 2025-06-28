using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace AAEmu.Commons.Network.Core;

public class Session(Server server) : TcpSession(server), ISession
{
    private readonly Dictionary<string, object> _attributes = [];

    public IBaseProtocolHandler ProtocolHandler { get; } = server.GetHandler();
    public IPEndPoint RemoteEndPoint { get; private set; }
    public uint SessionId { get; private set; }
    public IPAddress Ip { get; private set; }

    protected override void OnConnecting()
    {
        RemoteEndPoint = (IPEndPoint)Socket.RemoteEndPoint;
        SessionId = (uint)RemoteEndPoint.GetHashCode();
        Ip = RemoteEndPoint.Address;
        ProtocolHandler?.OnConnect(this);
    }

    protected override void OnConnected()
    {
        // Moved to OnConnecting due to a bug in TcpSession where OnReceived can happen before OnConnected.
        //_remoteEndPoint = (IPEndPoint)Socket.RemoteEndPoint;
        //_sessionId = (uint)RemoteEndPoint.GetHashCode();
        //_ip = RemoteEndPoint.Address;
        //ProtocolHandler?.OnConnect(this);
    }

    protected override void OnDisconnected()
    {
        ProtocolHandler?.OnDisconnect(this);
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        ProtocolHandler?.OnReceive(this, buffer, (int)offset, (int)size);
    }

    protected override void OnSent(long sent, long pending)
    {
    }

    protected override void OnError(SocketError error)
    {
    }

    public void SendPacket(ReadOnlySpan<byte> packet)
    {
        SendAsync(packet);
    }

    public bool TrySend(PacketStream packet)
    {
        throw new NotImplementedException();
    }

    public ValueTask SendAsync(PacketStream packet, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void AddAttribute(string name, object attribute) => _attributes.Add(name, attribute);

    public object GetAttribute(string name)
    {
        _attributes.TryGetValue(name, out var attribute);
        return attribute;
    }

    public void ClearAttribute(string name) => _attributes.Remove(name);

    public void Close() => Disconnect();
}
